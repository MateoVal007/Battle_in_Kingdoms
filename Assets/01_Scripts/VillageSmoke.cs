#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
public class VillageSmoke : MonoBehaviour {
 readonly List<string> results=new List<string>();Keyboard keyboard;VillageGame game;InputSettings priorInput,verificationInput;
 const string Folder="C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/Pueblo_verificacion";
 void Check(bool pass,string description){results.Add((pass?"PASS ":"FAIL ")+description);File.WriteAllLines(Folder+"/progress.txt",results);}
 void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
 IEnumerator Start(){
 // Keep synthetic keyboard events routed to Play Mode in batch/background verification.
 priorInput=InputSystem.settings;verificationInput=Instantiate(priorInput);InputSystem.settings=verificationInput;
 verificationInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
 verificationInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 Directory.CreateDirectory(Folder);keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();game=FindAnyObjectByType<VillageGame>();
 yield return new WaitForSeconds(.8f);
 Check(game&&game.warrior&&game.mage,"Escena y dos personajes inicializados");
 var w=game.warrior;var m=game.mage;
 float x=w.transform.position.x;Keys(Key.D);yield return new WaitForSeconds(.5f);Keys();yield return null;Check(w.transform.position.x>x+2,"Movimiento horizontal del guerrero");
 float y=w.transform.position.y;Keys(Key.W);yield return new WaitForSeconds(.2f);Keys();Check(w.transform.position.y>y+.7f,"Salto del guerrero");yield return new WaitForSeconds(.9f);
 Check(!w.hasEquipment&&w.armor==0,"Guerrero empieza sin equipo");
 w.Teleport(new Vector3(4.4f,1.3f,0));Keys(Key.F);yield return new WaitForSeconds(.2f);Keys();
 m.Teleport(new Vector3(6,1.3f,0));yield return new WaitForSeconds(.2f);Check(!w.hasEquipment&&!m.hasEquipment,"Mago no recoge equipo del guerrero");
 w.Teleport(new Vector3(6,1.3f,0));yield return new WaitForSeconds(.3f);Check(w.hasEquipment&&w.armor==60,"Puñetazo abre cofre inicial y recogida equipa espada escudo armadura");
 m.Teleport(new Vector3(18,1.3f,0));yield return new WaitForSeconds(.3f);Check(m.fireCharge==25&&m.enchantment=="Ninguno","Recoger libro guarda fuego sin gastarlo");
 Keys(Key.R);yield return null;Keys();yield return new WaitForSeconds(.2f);Check(m.enchantment=="Fuego"&&m.fireCharge<25,"Activación manual de fuego");
 w.Teleport(new Vector3(36,1.3f,0));w.immuneUntil=0;int before=w.health+w.armor;yield return new WaitForSeconds(.4f);Check(w.health+w.armor<before,"Espinos aplican daño real por contacto");
 w.Teleport(new Vector3(84,-.5f,0));w.immuneUntil=0;before=w.health+w.armor;yield return new WaitForSeconds(.4f);Check(w.health+w.armor<before,"Veneno aplica daño por contacto");
 w.Teleport(new Vector3(99,1.3f,0));yield return new WaitForSeconds(.3f);Check(game.checkpoint>=99,"Checkpoint registra avance");
 w.immuneUntil=0;w.Damage(1000);yield return new WaitForSeconds(.2f);
 Check(w.IsDown&&w.health==0,"Derrota individual no reaparece automáticamente");
 float downX=w.transform.position.x;Keys(Key.D,Key.F,Key.W);yield return new WaitForSeconds(.3f);Keys();
 Check(Mathf.Abs(w.transform.position.x-downX)<.1f&&!w.blocking,"Derribado no se mueve ni ataca");
 w.Teleport(new Vector3(103,1.3f,0));yield return new WaitForSeconds(.25f);Check(w.IsDown,"Derribado no recoge salud para reanimarse solo");
 int mageHealth=m.health;m.Teleport(new Vector3(103,1.3f,0));yield return new WaitForSeconds(.4f);
 Check(!w.IsDown&&w.health==50&&m.health==mageHealth,"Salud prioriza reanimar compañero con 50 HP (G="+w.health+", M="+m.health+", previo="+mageHealth+")");
 Check(Mathf.Abs(w.transform.position.x-m.transform.position.x)<3&&w.immuneUntil>Time.time,"Reanimación cercana y protegida");
 // A sword-opened chest must emit exactly one pickup and revive the partner.
 m.Teleport(new Vector3(3,1.3f,0));m.immuneUntil=0;m.Damage(1000);w.health=70;w.Teleport(new Vector3(18.4f,1.3f,0));
 yield return new WaitForSeconds(.4f);Keys(Key.F);yield return new WaitForSeconds(.2f);Keys();
 VillageChest swordChest=null;foreach(var c in FindObjectsByType<VillageChest>())if(Mathf.Abs(c.transform.position.x-20)<.1f)swordChest=c;
 Check(swordChest&&swordChest.opened,"Espada abre cofre de salud");
 int drops=0;foreach(var z in FindObjectsByType<VillageZone>(FindObjectsInactive.Include))if(z.spawnedByChest)drops++;
 yield return new WaitForSeconds(.35f);Keys(Key.F);yield return new WaitForSeconds(.2f);Keys();
 int afterDrops=0;foreach(var z in FindObjectsByType<VillageZone>(FindObjectsInactive.Include))if(z.spawnedByChest)afterDrops++;
 Check(drops==2&&afterDrops==2,"Cofre no duplica recompensas al volver a golpear");
 w.Teleport(new Vector3(20,1.3f,0));yield return new WaitForSeconds(.4f);
 Check(!m.IsDown&&m.health==50&&w.health==70,"Salud de cofre reanima sin curar al recolector (G="+w.health+", M="+m.health+")");
 m.Teleport(new Vector3(64.75f,6.8f,0));yield return new WaitForSeconds(.5f);Keys(Key.K);yield return new WaitForSeconds(.4f);Keys();
 bool shotChest=false;foreach(var c in FindObjectsByType<VillageChest>())if(c.transform.position.x>60&&c.transform.position.x<70)shotChest=c.opened;
 Check(shotChest,"Disparo del mago abre cofre elevado");
 // Attack against a real enemy with keyboard input.
 var enemy=FindAnyObjectByType<VillageEnemy>();enemy.transform.position=new Vector3(105,1.1f,0);enemy.originX=105;enemy.enabled=false;w.Teleport(new Vector3(103.4f,1.2f,0));int hp=enemy.health;Keys(Key.F);yield return new WaitForSeconds(.15f);Keys();Check(enemy.health<hp,"Espada daña enemigo en alcance");
 m.Teleport(new Vector3(101.5f,1.2f,0));w.Teleport(new Vector3(99,1.2f,0));hp=enemy.health;Keys(Key.K);yield return new WaitForSeconds(.4f);Keys();Check(!enemy||enemy.health<hp,"Disparo del mago impacta enemigo");
 w.Teleport(new Vector3(132,1.3f,0));m.Teleport(new Vector3(137,1.3f,0));Keys(Key.G);yield return new WaitForSeconds(.6f);Check(!game.gateOpen&&game.runeCharge==0,"Un jugador solo no abre la puerta");
 Keys(Key.G,Key.L);yield return new WaitForSeconds(3.4f);Keys();Check(game.gateOpen&&!game.gate.gameObject.activeSelf,"Escudo y canalización conjunta abren puerta");
 yield return NewAbilities(w,m);
 // Physical jump checks use the same keyboard inputs and CharacterController as the player.
 foreach(var e in FindObjectsByType<VillageEnemy>())e.enabled=false;
 w.immuneUntil=Time.time+100;
 w.Teleport(new Vector3(8,1.3f,0));yield return new WaitForSeconds(.4f);Keys(Key.D);yield return new WaitForSeconds(.5f);Keys();Check(w.transform.position.x<8.2f,"Bloque inicial sólido impide caminar a través");
 yield return JumpTo(w,10,1.225f,"Salta sobre el primer bloque sólido");
 yield return JumpTo(w,15,2.225f,"Salta sobre el segundo bloque sólido");
 w.Teleport(new Vector3(24,1.3f,0));yield return new WaitForSeconds(.5f);
 yield return JumpTo(w,27,2.225f,"Huertos escalón 1");
 yield return JumpTo(w,31,4.425f,"Huertos escalón 2");
 yield return JumpTo(w,34.5f,5.725f,"Huertos ruta alta");
 yield return WalkTo(w,38.3f);
 yield return JumpTo(w,43,3.425f,"Huertos descenso");
 w.Teleport(new Vector3(47,1.3f,0));yield return new WaitForSeconds(.5f);
 yield return JumpTo(w,50,1.525f,"Tejados acceso");
 yield return JumpTo(w,54,3.425f,"Tejados subida");
 yield return JumpTo(w,58,5.525f,"Tejados primer techo");
 yield return WalkTo(w,60.5f);
 yield return JumpTo(w,64.6f,5.525f,"Tejados segundo techo");
 w.Teleport(new Vector3(74,1.3f,0));yield return new WaitForSeconds(.5f);
 yield return WalkTo(w,78);Check(w.Grounded&&Mathf.Abs(w.transform.position.x-78)<.4f,"Orilla libre de plataformas que bloqueen el acceso");
 yield return JumpTo(w,83,1.625f,"Arroyo plataforma baja 2");
 yield return JumpTo(w,88,1.025f,"Arroyo plataforma baja 3");
 yield return JumpTo(w,91,0.12f,"Arroyo salida segura");
 w.Teleport(new Vector3(70,3.5f,0));yield return new WaitForSeconds(.5f);
 yield return JumpTo(w,72,2.225f,"Arroyo escalon de acceso alto");
 yield return JumpTo(w,75,4.225f,"Arroyo acceso ruta alta");
 yield return WalkTo(w,76.15f);
 yield return JumpTo(w,78,5.625f,"Arroyo escalón intermedio alto");
 yield return JumpTo(w,82,6.525f,"Arroyo salto alto");
 yield return WalkTo(w,83.4f);
 yield return JumpTo(w,87,6.525f,"Arroyo ruta alta recompensa");
 w.Teleport(new Vector3(84,-.6f,0));w.health=100;w.armor=0;w.immuneUntil=0;yield return new WaitForSeconds(.6f);
 Check(w.health<100&&!w.IsDown,"Caer al veneno daña sin muerte inmediata");
 yield return WalkTo(w,85.1f);
 yield return JumpTo(w,86.4f,-.575f,"Salir del veneno a piedra de rescate");
 yield return JumpTo(w,88,1.025f,"Recuperar ruta desde piedra de rescate");
 yield return JumpTo(w,91,.12f,"Volver a tierra después del veneno");
 Check(!w.IsDown&&w.health>0,"Escape del veneno conserva al jugador vivo");
 w.immuneUntil=0;m.immuneUntil=0;w.Damage(1000);m.Damage(1000);yield return new WaitForSeconds(.3f);
 Check(game.BothDown,"Derrota conjunta espera reinicio explícito");
 game.RestartPair();yield return new WaitForSeconds(.2f);Check(w.health==100&&m.health==100&&!game.BothDown,"Reinicio conjunto restaura equipo y recursos");
 w.immuneUntil=0;w.health=100;w.armor=40;m.health=100;game.gateOpen=false;game.runeCharge=0;game.gate.gameObject.SetActive(true);
 for(int i=0;i<8;i++){game.JumpSection(i);yield return new WaitForSeconds(.5f);game.view.transform.position=new Vector3(i*24+11,5.8f,-22);Capture(game.view,Folder+"/sector-"+(i+1).ToString("00")+".png");}
 game.warrior.Teleport(new Vector3(187,1.2f,0));game.mage.Teleport(new Vector3(189,1.2f,0));yield return null;Check(game.completed,"Salida requiere y reconoce ambos personajes");
 File.WriteAllLines(Folder+"/resultados.txt",results);Keys();InputSystem.RemoveDevice(keyboard);if(priorInput)InputSystem.settings=priorInput;if(verificationInput)Destroy(verificationInput);EditorApplication.isPlaying=false;
 }
 IEnumerator WalkTo(VillageHero hero,float x){float end=Time.time+2;while(Time.time<end&&Mathf.Abs(hero.transform.position.x-x)>.12f){Keys(hero.transform.position.x<x?Key.D:Key.A);yield return null;}Keys();yield return new WaitForSeconds(.12f);}
 IEnumerator JumpTo(VillageHero hero,float x,float surface,string label){
 float start=Time.time;Keys(Key.W,hero.transform.position.x<x?Key.D:Key.A);yield return null;
 while(Time.time-start<1.3f){float dx=x-hero.transform.position.x;Keys(Mathf.Abs(dx)>.12f?new[]{Key.W,dx>0?Key.D:Key.A}:new[]{Key.W});yield return null;}
 Keys();yield return new WaitForSeconds(.12f);
 Check(Mathf.Abs(hero.transform.position.x-x)<.6f&&Mathf.Abs((hero.transform.position.y-1)-surface)<.3f&&hero.Grounded,label+" (x="+hero.transform.position.x.ToString("F1")+", pies="+(hero.transform.position.y-1).ToString("F2")+")");
 }
 IEnumerator NewAbilities(VillageHero w,VillageHero m){
 foreach(var e in FindObjectsByType<VillageEnemy>())e.enabled=false;
 w.Teleport(new Vector3(20,1.3f,0));w.health=100;w.armor=0;w.immuneUntil=0;
 yield return new WaitForSeconds(.5f);float feet=w.transform.position.y-1;Keys(Key.S,Key.G);yield return new WaitForSeconds(.15f);
 var cc=w.GetComponent<CharacterController>();Check(w.crouching&&w.blocking&&cc.height<1.3f&&Mathf.Abs(cc.bounds.min.y-feet)<.15f,"Agacharse reduce colisión manteniendo los pies");
 int serial=w.AttackSerial;Keys(Key.S,Key.G,Key.F);yield return new WaitForSeconds(.2f);Check(w.AttackSerial>serial&&w.blocking&&w.crouching,"Atacar mientras se cubre agachado");
 int hp=w.health;w.Damage(20,true,w.transform.position.x+w.Facing*3,w.transform.position.y-.4f);Check(w.health==hp,"Escudo agachado bloquea ataque frontal");
 Keys(Key.G,Key.W);yield return new WaitForSeconds(.15f);Check(w.shieldUp&&w.Grounded&&!w.crouching,"Escudo superior sin salto accidental");
 hp=w.health;w.Damage(20,true,w.transform.position.x,w.transform.position.y+4);Check(w.health==hp,"Escudo superior bloquea ataque desde arriba");
 w.Damage(20,true,w.transform.position.x+w.Facing*3,w.transform.position.y);Check(w.health<hp,"Escudo superior deja descubierto el frente");
 Keys(Key.G);yield return new WaitForSeconds(.1f);w.immuneUntil=0;hp=w.health;w.Damage(20,true,w.transform.position.x,w.transform.position.y+4);Check(w.health<hp,"Escudo frontal no bloquea por encima");
 w.immuneUntil=0;hp=w.health;w.Damage(10);Check(w.health<hp,"Escudo no bloquea peligros ambientales");
 Keys(Key.S);yield return new WaitForSeconds(.15f);
 var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=new Vector3(20,1.7f,0);roof.transform.localScale=new Vector3(3,.3f,2);Physics.SyncTransforms();Keys();yield return new WaitForSeconds(.15f);
 Check(w.crouching&&cc.height<1.3f,"No se levanta atravesando un techo bajo");Destroy(roof);yield return new WaitForSeconds(.2f);Check(!w.crouching&&cc.height>1.9f,"Recupera postura al tener espacio");
 m.Teleport(new Vector3(22,1.3f,0));m.health=100;m.fireCharge=25;m.iceCharge=25;m.protectionCharge=25;m.selectedItem=0;m.itemActive=false;m.enchantment="Ninguno";
 yield return new WaitForSeconds(.3f);Check(m.fireCharge==25,"Inventario conserva carga cuando no se usa");
 Keys(Key.R);yield return new WaitForSeconds(.4f);Keys();float fire=m.fireCharge;Check(m.itemActive&&fire<25&&fire>24,"Carga circular consume tiempo solo al activar");
 Keys(Key.E);yield return null;Keys();yield return new WaitForSeconds(.2f);Check(m.selectedItem==1&&!m.itemActive&&Mathf.Abs(m.fireCharge-fire)<.1f,"Cambiar objeto guarda carga restante del anterior");
 Keys(Key.R);yield return null;Keys();yield return new WaitForSeconds(.15f);Keys(Key.K);yield return new WaitForSeconds(.05f);Keys();bool ice=false;foreach(var bolt in FindObjectsByType<VillageBolt>())if(bolt.ice)ice=true;Check(ice&&m.iceCharge<25,"Hielo seleccionado modifica el disparo");
 Keys(Key.E);yield return null;Keys();yield return null;Keys(Key.R);yield return null;Keys();yield return new WaitForSeconds(.1f);m.immuneUntil=0;m.armor=0;hp=m.health;m.Damage(20);Check(m.health==hp-10,"Protección seleccionada reduce daño a la mitad");
 m.protectionCharge=.05f;yield return new WaitForSeconds(.15f);Check(m.protectionCharge==0&&!m.itemActive&&m.enchantment=="Ninguno","Objeto agotado se desactiva");
 Keys(Key.R);yield return null;Keys();yield return null;Check(!m.itemActive,"No activa un objeto vacío");
 Keys(Key.Q);yield return null;Keys();yield return null;Check(m.selectedItem==1,"Selección anterior del inventario");
 w.health=100;w.immuneUntil=Time.time+100;
 w.Teleport(new Vector3(29,2.4f,0));yield return new WaitForSeconds(.5f);yield return WalkTo(w,32);Check(w.Grounded&&Mathf.Abs(w.transform.position.x-32)<.5f&&w.transform.position.y>2.3f,"Sube ladera sin saltar");
 yield return WalkTo(w,39);Check(w.Grounded&&Mathf.Abs(w.transform.position.x-39)<.5f&&w.transform.position.y<.6f,"Desciende a hondonada sin quedarse atascado");
 w.Teleport(new Vector3(151,3.4f,0));yield return new WaitForSeconds(.5f);yield return WalkTo(w,154);Check(w.Grounded&&w.transform.position.y>3.2f,"Camino principal asciende por la colina del bosque");
 }
 static void Capture(Camera camera,string path){var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=prior;Destroy(tex);Destroy(rt);}
}
#endif










