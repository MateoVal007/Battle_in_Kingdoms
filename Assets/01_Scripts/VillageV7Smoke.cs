#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public class VillageV7Smoke:MonoBehaviour
{
 readonly List<string> checks=new List<string>();VillageGame game;VillageHero w,m;
 void Check(bool pass,string text){checks.Add((pass?"PASS ":"FAIL ")+text);File.WriteAllLines("Docs/Verification/v7-play-results.txt",checks);}
 IEnumerator Step(VillageHero h,float duration,float move=0)
 {float end=Time.time+duration;while(Time.time<end){h.Simulate(move,false,false,false,Time.deltaTime);yield return null;}}
 static void Capture(Camera c,Vector3 position,string name)
 {var old=c.transform.position;c.transform.position=position;var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1280,720,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes("C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/"+name,t.EncodeToPNG());c.targetTexture=null;c.transform.position=old;RenderTexture.active=prior;Destroy(t);Destroy(rt);}
 IEnumerator Start()
 {
  Application.runInBackground=true;Time.timeScale=1;yield return new WaitForSeconds(.4f);
  game=FindAnyObjectByType<VillageGame>();w=game.warrior;m=game.mage;w.enabled=m.enabled=false;
  foreach(var e in FindObjectsByType<VillageEnemy>())e.gameObject.SetActive(false);
  var stars=FindObjectsByType<VillageStar>().OrderBy(s=>s.id).ToArray();foreach(var s in stars){s.GetComponent<Collider>().enabled=false;s.mask=0;}
  Check(stars.Length>=150&&stars.Length<=VillageStar.Capacity,"Estrellas suficientes para rutas y una vida por jugador: "+stars.Length);
  Check(stars.Select(s=>s.id).Distinct().Count()==stars.Length,"Identificadores únicos para sincronizar estrellas");
  w.Teleport(new Vector3(118,1.3f,0));m.Teleport(new Vector3(120,1.3f,0));w.stars=m.stars=w.extraLives=m.extraLives=0;w.health=m.health=100;
  for(int i=0;i<99;i++)stars[i].TryCollect(w);
  Check(w.stars==99&&w.extraLives==0&&m.stars==0,"99 estrellas del guerrero no cambian contador del mago");
  Check(!stars[0].TryCollect(w)&&w.stars==99,"La misma estrella no se cuenta dos veces");stars[99].TryCollect(w);
  Check(w.stars==0&&w.extraLives==1,"100 estrellas dan una vida y reinician contador");
  for(int i=0;i<100;i++)stars[i].TryCollect(m);
  Check(m.stars==0&&m.extraLives==1&&stars.Take(100).All(s=>s.mask==3),"Mago recoge sus propias copias y obtiene su vida");
  w.health=0;Check(!stars[100].TryCollect(w),"Derribado no recoge estrellas");
  Check(w.TryUseExtraLife()&&w.health==100&&w.extraLives==0,"Vida extra reanima desde checkpoint");
  Check(w.RecoveryRemaining>2.8f,"Reanimación concede tres segundos de protección");
  Check(!w.TryUseExtraLife()&&w.extraLives==0,"No gasta vida estando vivo ni duplica reanimación");
  w.health=0;w.extraLives=1;Check(game.CollectHealth(m)&&w.health==50&&w.extraLives==1,"Salud del compañero sigue reanimando sin gastar vida extra");
  w.Teleport(new Vector3(118,1.3f,0));w.health=100;w.armor=0;w.hasEquipment=false;w.immuneUntil=0;
  w.Damage(10);Check(w.health==90&&w.RecoveryRemaining>2.9f,"Daño inicia recuperación de tres segundos");w.Damage(10);
  Check(w.health==90,"No recibe daño acumulado inmediatamente");yield return new WaitForSeconds(1.1f);w.Damage(10);
  Check(w.health==90&&w.RecoveryRemaining>1.7f,"Sigue invulnerable tras un segundo");yield return new WaitForSeconds(2.1f);w.Damage(10);
  Check(w.health==80,"Vuelve a recibir daño pasados los tres segundos");
  var fixture=stars.Last();fixture.mask=0;fixture.transform.position=new Vector3(118,1.2f,0);fixture.GetComponent<Collider>().enabled=true;
  w.stars=0;w.Teleport(new Vector3(116.5f,1.3f,0));yield return Step(w,.4f,1);
  Check(w.stars==1&&(fixture.mask&1)!=0,"Recogida física con CharacterController y trigger");fixture.GetComponent<Collider>().enabled=false;
  w.health=100;w.armor=0;w.hasEquipment=false;w.immuneUntil=0;
  var enemy=FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include).First(e=>!e.whip&&!e.ranged);enemy.gameObject.SetActive(true);enemy.enabled=false;enemy.SetHome(new Vector3(118,1.15f,0));int hp=enemy.health;
  w.Teleport(new Vector3(118,5.5f,0));yield return Step(w,1.2f);
  Check(w.health<100,"Caer encima de enemigo causa daño de contacto");Check(enemy.health==hp,"El salto no mata ni daña al enemigo como un pisotón");int heroHp=w.health;yield return Step(w,.8f);
  Check(w.health==heroHp,"Permanecer en contacto respeta la recuperación");enemy.gameObject.SetActive(false);
  foreach(var s in stars)s.mask=0;
  m.Teleport(new Vector3(120,1.3f,0));m.health=100;m.selectedItem=2;m.protectionCharge=25;m.itemActive=false;m.ToggleItem();m.Simulate(0,false,false,false,.016f);
  Check(m.ProtectionVisible,"Campo esférico visible con protección activa");
  var bubble=GameObject.Find("Campo de protección");Check(bubble&&bubble.transform.localScale.x>2&&bubble.GetComponent<Renderer>().sharedMaterial.color.a<.5f,"Esfera redonda translúcida rodea el cuerpo");
  Capture(game.view,new Vector3(120,3.3f,-14),"v7-campo-proteccion.png");
  m.SelectItem(1);m.Simulate(0,false,false,false,.016f);Check(!m.ProtectionVisible,"Cambiar magia oculta el campo");
  m.selectedItem=2;m.protectionCharge=.01f;m.ToggleItem();m.Simulate(0,false,false,false,.1f);Check(!m.ProtectionVisible,"Al agotarse protección desaparece el campo");
  Capture(game.view,new Vector3(81,5.8f,-26),"v7-estrellas-arroyo.png");
  stars[0].mask=1;w.stars=1;w.health=m.health=0;game.RestartPair();Check(stars[0].mask==1&&w.stars==1&&w.health==100&&m.health==100,"Reinicio conjunto no repone estrellas ya recogidas");
  Check(checks.All(c=>c.StartsWith("PASS")),"Resultado completo v7");Debug.Log(string.Join("\n",checks));EditorApplication.isPlaying=false;
 }
}
#endif
