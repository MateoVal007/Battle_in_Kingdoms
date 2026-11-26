#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public class VillageV6Smoke:MonoBehaviour
{
 readonly List<string> checks=new List<string>();VillageGame game;VillageHero hero;
 void Check(bool pass,string text){checks.Add((pass?"PASS ":"FAIL ")+text);File.WriteAllLines("Docs/Verification/v6-play-results.txt",checks);}
 IEnumerator Step(float seconds,float move=0,bool jump=false)
 {float end=Time.time+seconds;bool first=true;while(Time.time<end){hero.Simulate(move,first&&jump,false,false,Time.deltaTime);first=false;yield return null;}}
 IEnumerator Jump(float x,float top)
 {
  float end=Time.time+1.4f;bool first=true;
  while(Time.time<end){float dx=x-hero.transform.position.x;hero.Simulate(Mathf.Abs(dx)>.1f?Mathf.Sign(dx):0,first,false,false,Time.deltaTime);first=false;yield return null;}
  Check(Mathf.Abs(hero.transform.position.x-x)<.55f&&Mathf.Abs(hero.transform.position.y-1-top)<.25f&&hero.Grounded,"Salto a "+x+" / cota "+top+" (real "+hero.transform.position.ToString("F2")+")");
 }
 IEnumerator Route(float start,params Vector2[] points)
 {hero.Teleport(new Vector3(start,VillageTerrainProfile.Height(start)+1.3f,0));yield return Step(.5f);foreach(var p in points)yield return Jump(p.x,p.y);}
 static Vector2 P(float x,float y)=>new Vector2(x,y);
 static void Capture(Camera c,string name,float x,float y)
 {c.transform.position=new Vector3(x,y,name.Contains("arroyo")?-26:-22);var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/"+name,tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=prior;Destroy(tex);Destroy(rt);}
 IEnumerator Start()
 {
  Application.runInBackground=true;Time.timeScale=1;EditorApplication.isPaused=false;
  yield return new WaitForSeconds(.5f);game=FindAnyObjectByType<VillageGame>();hero=game.warrior;
  hero.enabled=game.mage.enabled=false;game.mage.Teleport(new Vector3(3,1.3f,0));hero.immuneUntil=Time.time+200;
  foreach(var e in FindObjectsByType<VillageEnemy>())e.gameObject.SetActive(false);
  foreach(var c in FindObjectsByType<VillageChest>())c.Hit();
  yield return Route(7,P(10,1.4f),P(14,2.8f));
  yield return Route(24,P(27,1.5f),P(31,2.9f),P(35,4.3f),P(39,4.3f),P(43,4.3f));
  yield return Route(47,P(50,1.5f),P(54,2.9f),P(58,4.3f),P(62,4.3f),P(66,2.8f),P(68.5f,1.4f),P(71.5f,2.8f),P(74.5f,4.2f),P(78.5f,5.6f),P(82.5f,6.4f),P(86.5f,6.4f),P(90.5f,6.4f),P(94.5f,5),P(98.5f,3.6f),P(104.5f,1.4f));
  hero.Teleport(new Vector3(82.5f,7.6f,0));yield return Step(.4f);Capture(game.view,"v6-arroyo-rutas.png",81,5.8f);
  yield return Route(77,P(81,.6f),P(85,-.6f),P(89,.6f),P(92,.12f));
  hero.Teleport(new Vector3(82.9f,-.7f,0));yield return Step(.5f);yield return Jump(85,-.6f);yield return Jump(89,.6f);
  yield return Route(120,P(123,1.4f),P(127,2.8f));
  yield return Route(143,P(145,1.4f),P(149,2.8f),P(153,4.2f),P(157,5.6f),P(161,4.2f),P(165,2.8f));
  yield return Route(169,P(172,1.5f),P(176,2.9f),P(180,4.3f),P(184,2.9f),P(188,1.4f));
  yield return Route(192,P(196,1.4f),P(200,2.8f),P(204,4.2f),P(208,2.8f),P(212,1.4f));
  yield return Route(216,P(220,1.4f),P(224,2.8f),P(228,4.2f),P(232,2.8f),P(236,1.4f));
  Check(FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include).Length>=17,"Diez enemigos adicionales distribuidos por el nivel");
  Check(FindObjectsByType<VillageChest>().Count(c=>c.reward=="Armadura")>=6,"Seis cofres nuevos para recuperar equipo");
  Capture(game.view,"v6-camino-real.png",218,5.8f);
  hero.Teleport(new Vector3(234,1.3f,0));game.mage.Teleport(new Vector3(236,1.3f,0));yield return null;Check(game.completed,"Salida reconoce ambos jugadores en sección 10");game.completed=false;
  game.mage.Teleport(new Vector3(3,1.3f,0));
  hero.Teleport(new Vector3(20,1.3f,0));yield return Step(.3f);hero.health=100;hero.armor=60;hero.hasEquipment=true;hero.immuneUntil=0;
  for(int i=0;i<6;i++){hero.Simulate(0,false,false,true,.016f);hero.immuneUntil=0;hero.Damage(20,true,hero.transform.position.x+hero.Facing*3,hero.transform.position.y);}
  Check(hero.health==100&&hero.armor==0&&!hero.hasEquipment&&!hero.blocking,"Seis bloqueos gastan equipo; pierde espada y escudo sin daño de salud");
  hero.immuneUntil=0;hero.Damage(20,true,hero.transform.position.x+3,hero.transform.position.y);Check(hero.health==80,"Sin equipo ya no bloquea el siguiente golpe");
  hero.armor=60;hero.hasEquipment=true;for(int i=0;i<20;i++)hero.Simulate(0,false,false,true,.016f);Check(hero.armor==60,"Mantener escudo sin impactos no consume resistencia");
  hero.immuneUntil=0;hero.Damage(60,false);Check(!hero.hasEquipment,"Daño que rompe armadura también retira espada y escudo");
  var archer=FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include).Single(e=>e.ranged);archer.gameObject.SetActive(true);
  hero.Teleport(new Vector3(47.3f,1.3f,0));hero.immuneUntil=Time.time+20;int before=archer.AttackCount;yield return new WaitForSeconds(1.1f);
  var arrows=FindObjectsByType<VillageEnemyBolt>();Check(archer.AttackCount>before&&arrows.Any(b=>b.arrow&&b.velocity.y<0),"Arquero dispara hacia abajo a jugador inferior");
  Check(arrows.All(b=>!b.arrow||b.velocity.y<0),"Todas las flechas tienen velocidad descendente");
  hero.Teleport(new Vector3(43,11,0));game.mage.Teleport(new Vector3(3,1.3f,0));before=archer.AttackCount;yield return new WaitForSeconds(2.5f);Check(archer.AttackCount==before,"No dispara hacia arriba ni al jugador a su misma altura");
  hero.Teleport(new Vector3(39,5.5f,0));Capture(game.view,"v6-arquero-altura.png",41,6.8f);
  archer.gameObject.SetActive(false);
  Check(archer.transform.position.y>=8.8f,"Arquero elevado sobre la montaña");
  // Measure the full jumping body underneath the bridge, rather than only landing heights.
  hero.Teleport(new Vector3(81,1.8f,0));yield return Step(.4f);float baseY=hero.transform.position.y,maxY=baseY;float end=Time.time+.9f;bool first=true;
  while(Time.time<end){hero.Simulate(0,first,false,false,Time.deltaTime);first=false;maxY=Mathf.Max(maxY,hero.transform.position.y);yield return null;}
  Check(maxY-baseY>2,"Salto completo bajo la pasarela sin golpear techo");
  var mage=game.mage;mage.Teleport(new Vector3(192,1.3f,0));mage.fireCharge=mage.iceCharge=12;mage.protectionCharge=25;mage.selectedItem=0;mage.itemActive=false;mage.enchantment="Ninguno";
  mage.ToggleItem();Check(mage.fireCharge==11&&!mage.itemActive,"R lanza fuego y consume una carga");
  yield return new WaitForSeconds(.4f);mage.Simulate(0,false,false,false,.4f);Check(mage.fireCharge==11,"Fuego conserva cargas mientras espera");
  mage.Simulate(0,false,true,false,.016f);Check(mage.fireCharge==11,"Disparo básico no gasta cargas especiales");
  mage.SelectItem(1);yield return new WaitForSeconds(.4f);mage.ToggleItem();Check(mage.iceCharge==11,"R lanza hielo con una carga");
  mage.SelectItem(1);mage.ToggleItem();mage.Simulate(0,false,false,false,1);Check(mage.itemActive&&mage.protectionCharge==24,"Protección consume tiempo al activarse");
  mage.SelectItem(1);mage.Simulate(0,false,false,false,1);Check(!mage.itemActive&&mage.protectionCharge==24,"Cambiar encantamiento detiene protección y conserva su resto");
  mage.fireCharge=0;int serial=mage.AttackSerial;mage.ToggleItem();Check(mage.AttackSerial==serial,"Arma vacía no dispara");
  hero.Teleport(new Vector3(3,1.3f,0));mage.Teleport(new Vector3(5,1.3f,0));
  var patrol=FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include).First(e=>!e.whip&&!e.ranged);patrol.gameObject.SetActive(true);patrol.enabled=true;patrol.SetHome(new Vector3(226,1.15f,0));float origin=patrol.transform.position.x;
  yield return new WaitForSeconds(1.2f);Check(Mathf.Abs(patrol.transform.position.x-origin)>.5f&&patrol.State=="Patrulla","Enemigo patrulla sin jugadores cerca");patrol.gameObject.SetActive(false);
  var boss=FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include).Single(e=>e.whip);boss.health=240;boss.gameObject.SetActive(true);boss.SetHome(new Vector3(114,1.65f,0));hero.Teleport(new Vector3(119.5f,1.3f,0));
  yield return new WaitForSeconds(1.2f);Check(boss.transform.position.x>114.3f,"Guardián persigue dentro de su zona");hero.Teleport(new Vector3(140,1.3f,0));yield return new WaitForSeconds(2.8f);Check(boss.HomeReturns>0&&Mathf.Abs(boss.transform.position.x-114)<2.5f,"Guardián vuelve al puesto cuando el jugador se aleja");
  Check(checks.All(c=>c.StartsWith("PASS")),"Resultado completo v6");Debug.Log(string.Join("\n",checks));EditorApplication.isPlaying=false;
 }
}
#endif
