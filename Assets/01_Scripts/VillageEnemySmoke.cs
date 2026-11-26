#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public class VillageEnemySmoke:MonoBehaviour
{
 readonly List<string> lines=new List<string>();
 void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines("Docs/Verification/enemy-play-results.txt",lines);}
 static void Capture(Camera camera,float x,string name)
 {
  camera.transform.position=new Vector3(x,5.8f,-22);var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;
  camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
  tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/"+name,tex.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=prior;Destroy(tex);Destroy(rt);
 }
 void Place(VillageEnemy e,Vector3 p){var cc=e.GetComponent<CharacterController>();if(cc)cc.enabled=false;e.transform.position=p;if(cc&&!e.ranged)cc.enabled=true;Physics.SyncTransforms();}
 IEnumerator Start()
 {
  yield return new WaitForSeconds(.5f);
  var game=FindAnyObjectByType<VillageGame>();var w=game.warrior;var m=game.mage;
  var all=FindObjectsByType<VillageEnemy>();var archer=all.Single(e=>e.ranged);var whip=all.Single(e=>e.whip);var chaser=all.First(e=>!e.ranged&&!e.whip);
  Check(whip.health==240&&whip.transform.localScale.y>1,"Guardián grande en plaza con 240 HP");
  Check(Mathf.Abs(whip.transform.position.x-114)<.2f&&game.checkpoint<114,"Guardián tras checkpoint y antes del molino");
  foreach(var e in all)e.gameObject.SetActive(false);
  w.enabled=m.enabled=false;w.health=m.health=100;w.immuneUntil=m.immuneUntil=Time.time+100;w.armor=m.armor=0;
  w.Teleport(new Vector3(99,1.2f,0));m.Teleport(new Vector3(137,1.2f,0));
  chaser.gameObject.SetActive(true);Place(chaser,new Vector3(108,1.15f,0));chaser.originX=108;
  yield return new WaitForSeconds(2.5f);
  Check(chaser.transform.position.x<104,"Persigue más allá del antiguo límite de 4 m (x="+chaser.transform.position.x+")");
  Check(chaser.Target==w,"Elige jugador vivo y mantiene objetivo");
  w.health=0;m.Teleport(new Vector3(108,1.2f,0));yield return new WaitForSeconds(.2f);
  Check(chaser.Target==m,"Ignora derribado y persigue compañero vivo");
  chaser.gameObject.SetActive(false);w.health=100;w.Teleport(new Vector3(38.5f,.5f,0));m.Teleport(new Vector3(100,1.2f,0));
  archer.gameObject.SetActive(true);Vector3 post=archer.transform.position;
  yield return new WaitForSeconds(.2f);Check(archer.AttackVisual>0,"Arquero avisa antes de disparar");
  Capture(game.view,40,"enemy-archer-preview.png");yield return new WaitForSeconds(.6f);Check(FindObjectsByType<VillageEnemyBolt>().Any(b=>b.arrow),"Arquero dispara flechas desde altura");
  yield return new WaitForSeconds(.5f);Check(Vector3.Distance(post,archer.transform.position)<.001f,"Arquero permanece fijo en su montaña");
  archer.gameObject.SetActive(false);foreach(var b in FindObjectsByType<VillageEnemyBolt>())Destroy(b.gameObject);
  w.Teleport(new Vector3(111.2f,1.2f,0));w.immuneUntil=0;whip.gameObject.SetActive(true);
  yield return new WaitForSeconds(.25f);Check(whip.AttackVisual>0&&w.health==100,"Latigazo telegrafiado sin daño anticipado");
  Capture(game.view,113,"enemy-whip-preview.png");yield return new WaitForSeconds(.8f);Check(w.health==75,"Latigazo inflige 25 HP una vez");
  yield return new WaitForSeconds(.35f);Check(w.health==75,"No repite daño durante recuperación");
  w.health=100;w.immuneUntil=0;w.blocking=true;w.shieldUp=false;
  yield return new WaitForSeconds(2.5f);Check(w.health==100,"Escudo frontal bloquea látigo");
  whip.Hit(999,false);Check(!whip.gameObject.activeSelf,"Guardián derrotado deja de atacar");
  w.blocking=false;w.immuneUntil=Time.time+100;w.Teleport(new Vector3(82,-.5f,0));m.Teleport(new Vector3(100,1.2f,0));
  chaser.gameObject.SetActive(true);Place(chaser,new Vector3(75,1.15f,0));
  yield return new WaitForSeconds(2);Check(chaser.transform.position.x<79.1f,"Enemigo terrestre respeta borde del arroyo");
  chaser.gameObject.SetActive(false);w.Teleport(new Vector3(119,1.2f,0));Place(chaser,new Vector3(113,1.15f,0));chaser.gameObject.SetActive(true);
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(115,1.5f,0);wall.transform.localScale=new Vector3(.5f,3,3);Physics.SyncTransforms();
  yield return new WaitForSeconds(2);Check(chaser.transform.position.x<114.5f,"Perseguidor no atraviesa paredes");Destroy(wall);
  chaser.Hit(999,false);w.immuneUntil=0;w.Teleport(new Vector3(110,1.2f,0));w.blocking=true;w.shieldUp=true;
  var shot=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(shot.GetComponent<Collider>());shot.transform.position=w.transform.position+Vector3.up*2;
  var bolt=shot.AddComponent<VillageEnemyBolt>();bolt.arrow=true;bolt.velocity=Vector3.down*8;int hp=w.health;
  yield return new WaitForSeconds(.4f);Check(w.health==hp,"Escudo superior bloquea flecha descendente");
  Check(lines.All(s=>s.StartsWith("PASS")),"Resultado completo de enemigos");
  Debug.Log(string.Join("\n",lines));EditorApplication.isPlaying=false;
  EditorApplication.delayCall+=()=>EditorApplication.Exit(lines.Any(s=>s.StartsWith("FAIL"))?1:0);
 }
}
#endif

