using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class VillageRevisionV6
{
 static double next;
 static VillageRevisionV6(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=OnPlay;}
 static void Poll()
 {
  if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;next=EditorApplication.timeSinceStartup+2;
  if(!File.Exists("Docs/village-v6.request"))return;var mode=File.ReadAllText("Docs/village-v6.request").Trim();File.Delete("Docs/village-v6.request");ApplyBoth();
  if(mode=="test"){EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");SessionState.SetBool("VillageV6Test",true);EditorApplication.isPlaying=true;}
 }
 static void OnPlay(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillageV6Test",false)){SessionState.SetBool("VillageV6Test",false);new GameObject("Verificación v6").AddComponent<VillageV6Smoke>();}}
 public static void ApplyBoth()
 {
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  foreach(var path in new[]{"Assets/00_Scenes/Pueblo_01_Blockout.unity","Assets/00_Scenes/Pueblo_Online.unity"})
  {var scene=EditorSceneManager.OpenScene(path);var old=GameObject.Find("Revisión v6 · patrullas y camino real");if(old)Object.DestroyImmediate(old);VillageLayoutV5.Apply();Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  AssetDatabase.SaveAssets();Preview(81,5.8f,-26,"v6-arroyo-final.png");Preview(218,5.8f,-22,"v6-camino-final.png");File.WriteAllText("Docs/Verification/v6-layout.txt","Saved both scenes: 240 m, 10 sections.\n"+System.DateTime.UtcNow.ToString("s"));
 }
 static void Preview(float x,float y,float z,string name)
 {
  var c=Object.FindAnyObjectByType<VillageGame>().view;var old=c.transform.position;var prior=RenderTexture.active;var rt=new RenderTexture(1280,720,24);c.transform.position=new Vector3(x,y,z);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var t=new Texture2D(1280,720,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes("C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/"+name,t.EncodeToPNG());c.targetTexture=null;c.transform.position=old;RenderTexture.active=prior;Object.DestroyImmediate(t);Object.DestroyImmediate(rt);
 }
 static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_"+n+".mat");
 static GameObject Box(Transform root,string name,Vector3 p,Vector3 size,string mat,bool solid=true)
 {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root);o.transform.position=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=Mat(mat);if(!solid)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
 public static readonly Vector3[] River={new Vector3(68.5f,1.4f,2),new Vector3(71.5f,2.8f,2.4f),new Vector3(74.5f,4.2f,2.4f),new Vector3(78.5f,5.6f,3),new Vector3(82.5f,6.4f,3),new Vector3(86.5f,6.4f,3),new Vector3(90.5f,6.4f,3),new Vector3(94.5f,5,3),new Vector3(98.5f,3.6f,2.4f),new Vector3(104.5f,1.4f,2.4f)};
 public static readonly Vector3[] Extension={new Vector3(196,1.4f,3),new Vector3(200,2.8f,3),new Vector3(204,4.2f,3),new Vector3(208,2.8f,3),new Vector3(212,1.4f,3),new Vector3(220,1.4f,3),new Vector3(224,2.8f,3),new Vector3(228,4.2f,3),new Vector3(232,2.8f,3),new Vector3(236,1.4f,3)};
 static void Platform(Transform root,Vector3 p)
 {
  float ground=VillageTerrainProfile.Height(p.x)+.12f;var o=Box(root,"Plataforma de madera",new Vector3(p.x,p.y-.2f,0),new Vector3(p.z,.4f,2.5f),"Wood");o.AddComponent<VillageOneWayPlatform>();
  if(p.y-.4f-ground<2.25f)Box(root,"Terraplén v6",new Vector3(p.x,(p.y-.4f+ground)*.5f,0),new Vector3(p.z,Mathf.Max(.1f,p.y-.4f-ground),2.5f),"Earth");
  else for(int side=-1;side<=1;side+=2)Box(root,"Puntal v6",new Vector3(p.x+side*(p.z*.5f-.15f),(p.y-.4f+ground)*.5f,1.1f),new Vector3(.16f,p.y-.4f-ground,.16f),"Wood",false);
 }
 static void Label(Transform root,string text,float x,float y){var o=new GameObject(text);o.transform.SetParent(root);o.transform.position=new Vector3(x,y,-1.8f);var t=o.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=.055f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.15f,.2f,.23f);}
 public static void Apply()
 {
  var root=new GameObject("Revisión v6 · patrullas y camino real").transform;
  var previous=GameObject.Find("Diseño v5 · rutas de salto");
  foreach(Transform t in previous.transform.Cast<Transform>().ToArray())
  {
   float x=t.position.x;if(x<67||x>=100)continue;
   bool stone=Mathf.Abs(x-81)<.01f||Mathf.Abs(x-85)<.01f||Mathf.Abs(x-89)<.01f;
   bool keep=stone&&(t.name=="Piedra estrecha del arroyo"||((t.name=="Plataforma de madera"||t.name=="Borde claro")&&t.position.y<1.2f));
   if(!keep)Object.DestroyImmediate(t.gameObject);
  }
  foreach(var p in River)Platform(root,p);
  foreach(var z in Object.FindObjectsByType<VillageZone>())if(z.kind=="Salud"&&Mathf.Abs(z.transform.position.x-86.5f)<.1f)z.transform.position=new Vector3(86.5f,7,0);
  foreach(var label in Object.FindObjectsByType<TextMesh>())
  {
   if(label.text.StartsWith("04 · ARROYO")){label.text="04 · ARROYO\nPASARELA ALTA / PIEDRAS DE RESCATE";label.transform.position=new Vector3(83,9,-1.8f);}
   if(label.text.StartsWith("02 · ARQUERO"))label.transform.position=new Vector3(43,11,-1.8f);
   if(label.text=="Salud"&&label.transform.position.x>79&&label.transform.position.x<94)label.transform.position=new Vector3(86.5f,8,-1.8f);
   if(label.text.StartsWith("SALIDA")&&label.transform.position.x<200)label.transform.position+=Vector3.right*48;
   if(label.text=="08 · PUERTA")label.text="08 · PUERTA INTERIOR";
  }
  foreach(var e in Object.FindObjectsByType<VillageEnemy>())
  {e.detectionRange=e.ranged?14:e.whip?9:7;e.leashRadius=e.whip?10:11;e.patrolRadius=e.whip?2:2.5f;}
  var archer=Object.FindObjectsByType<VillageEnemy>().Single(e=>e.ranged);archer.transform.position=new Vector3(43,8.8f,0);
  var rock=GameObject.Find("Montaña del arquero · fondo");if(rock){rock.transform.position=new Vector3(43,3.3f,3.8f);rock.transform.localScale=new Vector3(5,11,2);}
  var perch=GameObject.Find("Cornisa del arquero · fondo");if(perch)perch.transform.position=new Vector3(43,7.7f,2.2f);
  foreach(var t in Object.FindObjectsByType<Transform>())if((t.name=="Limite derecho"||t.name.StartsWith("Arco salida")||t.name=="Dintel salida")&&t.position.x<200)t.position+=Vector3.right*48;
  Box(root,"Suelo camino real",new Vector3(203.5f,-1.5f,0),new Vector3(25,3,3),"Earth");Box(root,"Hierba camino real",new Vector3(203.5f,.06f,0),new Vector3(25,.12f,3),"Grass");
  Box(root,"Suelo patio",new Vector3(228,-1.5f,0),new Vector3(24,3,3),"Earth");Box(root,"Hierba patio",new Vector3(228,.06f,0),new Vector3(24,.12f,3),"Grass");
  foreach(var p in Extension)Platform(root,p);
  for(int i=0;i<4;i++)
  {
   float x=197+i*11;Box(root,"Casa camino real",new Vector3(x,2,7),new Vector3(6,4,3),"Plaster",false);
   Box(root,"Viga casa",new Vector3(x,2,5.35f),new Vector3(.16f,4,.18f),"DarkTimber",false);
   Box(root,"Viga horizontal casa",new Vector3(x,3,5.3f),new Vector3(6,.16f,.18f),"DarkTimber",false);
   Box(root,"Ventana casa",new Vector3(x-1.7f,2.5f,5.25f),new Vector3(1,1,.18f),"Blue",false);
   Box(root,"Puerta casa",new Vector3(x,1,5.4f),new Vector3(1,2,.2f),"Wood",false);
   foreach(int side in new[]{-1,1}){var roof=Box(root,"Tejado camino real",new Vector3(x+side*1.5f,4.6f,7),new Vector3(3.8f,.3f,4),"Roof",false);roof.transform.rotation=Quaternion.Euler(0,0,-side*25);}
  }
  foreach(float x in new[]{195f,206f,217f,229f,238f})
  {
   Box(root,"Tronco camino",new Vector3(x,2.5f,10),new Vector3(.45f,5,.5f),"Wood",false);
   var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.name="Copa camino real";crown.transform.SetParent(root);crown.transform.position=new Vector3(x,6,10);crown.transform.localScale=new Vector3(4,5,3);crown.GetComponent<Renderer>().sharedMaterial=Mat("Leaves");Object.DestroyImmediate(crown.GetComponent<Collider>());
  }
  foreach(float x in new[]{201f,225f})
  {var hill=GameObject.CreatePrimitive(PrimitiveType.Sphere);hill.name="Colina camino real";hill.transform.SetParent(root);hill.transform.position=new Vector3(x,1,24);hill.transform.localScale=new Vector3(31,15,4);hill.GetComponent<Renderer>().sharedMaterial=Mat("DistantHills");Object.DestroyImmediate(hill.GetComponent<Collider>());}
  Label(root,"09 · CAMINO REAL",203,8);Label(root,"10 · PATIO DE LA TORRE",228,8);
  var checkpoint=Box(root,"Checkpoint camino real",new Vector3(193,1,0),new Vector3(.6f,3,2),"Gold");checkpoint.GetComponent<Collider>().isTrigger=true;var cp=checkpoint.AddComponent<VillageZone>();cp.kind="Checkpoint";cp.checkpointX=193;
  var template=Object.FindObjectsByType<VillageChest>().First(c=>c.reward=="Equipo");
  foreach(float x in new[]{46f,101f,140f,168f,204f,228f})
  {var chest=Object.Instantiate(template.gameObject,root);chest.name="Cofre de armadura · "+x;chest.transform.position=new Vector3(x,(x==204||x==228?4.2f:VillageTerrainProfile.Height(x))+.85f,0);chest.GetComponent<VillageChest>().reward="Armadura";Label(root,"COFRE · ARMADURA",x,(x==204||x==228?4.2f:VillageTerrainProfile.Height(x))+2.7f);}
  foreach(float x in new[]{22f,65f,96f,110f,135f,190.5f,202f,214.5f,226f,238f})
  {
   var o=GameObject.CreatePrimitive(PrimitiveType.Capsule);o.name="Patrulla v6 · "+x;o.transform.SetParent(root);o.transform.position=new Vector3(x,VillageTerrainProfile.Height(x)+1.15f,0);o.transform.localScale=new Vector3(.85f,1,.85f);o.GetComponent<Renderer>().sharedMaterial=Mat("Danger");var e=o.AddComponent<VillageEnemy>();e.detectionRange=7;e.patrolRadius=2;e.leashRadius=10;
  }
  Physics.SyncTransforms();
  foreach(var e in Object.FindObjectsByType<VillageEnemy>())
  {
   if(e.ranged)continue;
   var hits=Physics.RaycastAll(new Vector3(e.transform.position.x,20,0),Vector3.down,30,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
   foreach(var hit in hits)if(!hit.collider.GetComponentInParent<VillageEnemy>()&&!hit.collider.GetComponentInParent<VillageHero>()&&!hit.collider.GetComponentInParent<VillageChest>())
   {e.transform.position=new Vector3(e.transform.position.x,hit.point.y+(e.whip?1.55f:1.05f),0);break;}
  }
  var count=Object.FindObjectsByType<VillageEnemy>().Length+Object.FindObjectsByType<VillageChest>().Length+Object.FindObjectsByType<VillageZone>().Count(z=>!z.spawnedByChest);
  if(count>64)throw new System.InvalidOperationException("World capacity: "+count);
 }
}
