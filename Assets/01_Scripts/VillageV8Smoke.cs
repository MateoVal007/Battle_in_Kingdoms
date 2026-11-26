#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class VillageV8Smoke:MonoBehaviour
{
 readonly List<string> checks=new List<string>();VillageGame game;VillageHero w,m;
 void Check(bool pass,string text){checks.Add((pass?"PASS ":"FAIL ")+text);File.WriteAllLines("Docs/Verification/v8-play-results.txt",checks);}
 IEnumerator Step(VillageHero h,float duration,bool crouch=false,bool attack=false,bool use=false)
 {float end=Time.time+duration;bool first=true;while(Time.time<end){h.Simulate(0,false,first&&attack,false,Time.deltaTime,crouch,false,0,first&&use);first=false;yield return null;}}
 IEnumerator Start()
 {
  Application.runInBackground=true;Time.timeScale=1;yield return new WaitForSeconds(.4f);game=FindAnyObjectByType<VillageGame>();w=game.warrior;m=game.mage;w.enabled=m.enabled=false;
  foreach(var e in FindObjectsByType<VillageEnemy>())e.gameObject.SetActive(false);
  var stars=FindObjectsByType<VillageStar>().OrderBy(s=>s.id).ToArray();foreach(var star in stars){star.GetComponent<Collider>().enabled=false;star.mask=0;}
  Check(stars.Length>=100&&stars.Length<=130,"Cantidad reducida en grupos concretos: "+stars.Length);
  w.health=m.health=100;w.stars=m.stars=w.extraLives=m.extraLives=0;
  Check(stars[0].TryCollect(w)&&!stars[0].TryCollect(m)&&w.stars==1&&m.stars==0,"Una estrella compartida solo cuenta para el primer jugador");
  yield return null;Check(!stars[0].GetComponentInChildren<Renderer>().enabled,"Estrella recogida desaparece para ambos");
  Check(stars[1].TryCollect(m)&&!stars[1].TryCollect(w)&&m.stars==1&&w.stars==1,"Mago recoge otra estrella para su propio contador");
  for(int i=2;i<stars.Length&&w.extraLives==0;i++)stars[i].TryCollect(w);
  Check(w.stars==0&&w.extraLives==1&&m.stars==1&&m.extraLives==0,"100 recogidas del guerrero dan vida solo al guerrero");
  m.Teleport(new Vector3(120,1.3f,0));m.health=100;m.armor=0;m.immuneUntil=0;m.protectionCharge=25;m.selectedItem=2;m.itemActive=false;m.ToggleItem();m.Simulate(0,false,false,false,.016f);
  m.Damage(20,true);m.Damage(50);Check(m.health==100&&m.ProtectionVisible,"Protección anula totalmente ataques y daño ambiental");
  var foe=FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include).First(e=>!e.whip&&!e.ranged);foe.gameObject.SetActive(true);foe.enabled=false;foe.SetHome(new Vector3(120,1.15f,0));m.Teleport(new Vector3(120,5.5f,0));yield return Step(m,1.2f);
  Check(m.health==100,"Protección también evita daño al caer sobre enemigo");foe.gameObject.SetActive(false);
  m.SelectItem(1);m.immuneUntil=0;m.Damage(10);Check(m.health==90,"Al cambiar de magia vuelve a recibir daño");
  m.Teleport(new Vector3(120,1.3f,0));yield return Step(m,.4f);m.health=100;m.immuneUntil=Time.time+10;
  yield return Step(m,.15f,true);Check(m.crouching&&m.GetComponent<CharacterController>().height<1.3f,"Mago se agacha con colisión reducida");
  m.Simulate(0,false,true,false,.016f,true);var shot=FindObjectsByType<VillageBolt>().OrderBy(b=>Vector3.Distance(b.transform.position,m.transform.position)).FirstOrDefault();
  Check(shot&&shot.transform.position.y<m.transform.position.y,"Disparo básico sale bajo mientras el mago está agachado");
  yield return new WaitForSeconds(.4f);m.selectedItem=0;m.fireCharge=12;m.Simulate(0,false,false,false,.016f,true,false,0,true);
  Check(m.crouching&&m.fireCharge==11,"Mago usa magia especial mientras se agacha");
  var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(120,1.8f,0);ceiling.transform.localScale=new Vector3(3,.2f,2);Physics.SyncTransforms();m.Simulate(0,false,false,false,.016f);
  Check(m.crouching,"No se levanta atravesando un techo");Destroy(ceiling);yield return Step(m,.2f);Check(!m.crouching,"Recupera postura cuando hay espacio");
  game.gateOpen=false;game.runeCharge=0;game.gate.gameObject.SetActive(true);w.Teleport(new Vector3(132,1.4f,0));m.Teleport(new Vector3(130,1.3f,0));yield return Step(w,.3f);yield return Step(m,.3f);
  Check(!game.gateOpen,"Un jugador solo en un piso no abre el paso");
  m.Teleport(new Vector3(137,5,0));yield return null;Check(!game.gateOpen,"Estar en el aire sobre el piso no abre el paso");
  m.Teleport(new Vector3(137,1.4f,0));yield return Step(w,.3f);yield return Step(m,.3f);yield return null;
  Check(game.gateOpen&&!game.gate.gameObject.activeSelf&&!w.blocking&&!m.casting,"Ambos de pie abren sin mantener botones ni esperar tres segundos");
  foreach(var star in stars)star.mask=0;w.stars=7;m.stars=3;w.Teleport(new Vector3(1,1.3f,0));m.Teleport(new Vector3(3,1.3f,0));game.active=0;game.view.transform.position=new Vector3(8,5.8f,-22);
  yield return Step(w,.5f);ScreenCapture.CaptureScreenshot("C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/v8-interfaz.png");yield return new WaitForSeconds(.3f);
  Check(checks.All(x=>x.StartsWith("PASS")),"Resultado completo v8");Debug.Log(string.Join("\n",checks));EditorApplication.isPlaying=false;
 }
}
#endif
