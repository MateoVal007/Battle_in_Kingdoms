using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class VillageLayoutV5
{
 static double next;
 static VillageLayoutV5(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=OnPlay;}
 static void Poll()
 {
  if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
  next=EditorApplication.timeSinceStartup+2;
  if(File.Exists("Docs/village-v5.request")){var mode=File.ReadAllText("Docs/village-v5.request").Trim();File.Delete("Docs/village-v5.request");ApplyBoth();if(mode=="apply-only")return;SessionState.SetBool("VillageV5Test",true);EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");EditorApplication.isPlaying=true;}
 }
 static void OnPlay(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillageV5Test",false)){SessionState.SetBool("VillageV5Test",false);new GameObject("Pruebas v5").AddComponent<VillageV5Smoke>();}}
 public static void ApplyBoth()
 {
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  foreach(var path in new[]{"Assets/00_Scenes/Pueblo_01_Blockout.unity","Assets/00_Scenes/Pueblo_Online.unity"}){var scene=EditorSceneManager.OpenScene(path);Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  AssetDatabase.SaveAssets();
  File.WriteAllText("Docs/Verification/v5-layout.txt","Saved local and online scenes · "+System.DateTime.UtcNow.ToString("s"));
  var camera=Object.FindAnyObjectByType<VillageGame>().view;var position=camera.transform.position;camera.transform.position=new Vector3(81,5.8f,-22);
  var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/v5-arroyo-final.png",tex.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=prior;camera.transform.position=position;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
 }
 static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_"+n+".mat");
 static GameObject Box(Transform root,string name,Vector3 p,Vector3 size,string mat,bool solid=true)
 {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root);o.transform.position=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=Mat(mat);if(!solid)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
 public static readonly Vector3[] Platforms={
  new Vector3(10,1.4f,3),new Vector3(14,2.8f,3),
  new Vector3(27,1.5f,3),new Vector3(31,2.9f,3),new Vector3(35,4.3f,3),new Vector3(39,4.3f,3),new Vector3(43,4.3f,3),
  new Vector3(50,1.5f,3),new Vector3(54,2.9f,3),new Vector3(58,4.3f,3),new Vector3(62,4.3f,3),new Vector3(66,2.8f,2.4f),
  new Vector3(68.5f,1.4f,2),new Vector3(71.5f,2.8f,2.4f),new Vector3(74.5f,4.2f,2.4f),new Vector3(78.5f,4.2f,3),new Vector3(82.5f,4.2f,3),new Vector3(86.5f,4.2f,3),new Vector3(90.5f,4.2f,3),new Vector3(94.5f,2.8f,2.4f),new Vector3(97.5f,1.4f,2.4f),
  new Vector3(81,.6f,2.2f),new Vector3(85,-.6f,2.2f),new Vector3(89,.6f,2.2f),
  new Vector3(123,1.4f,3),new Vector3(127,2.8f,3),
  new Vector3(145,1.4f,2.4f),new Vector3(149,2.8f,3),new Vector3(153,4.2f,3),new Vector3(157,5.6f,3),new Vector3(161,4.2f,3),new Vector3(165,2.8f,2.4f),
  new Vector3(172,1.5f,3),new Vector3(176,2.9f,3),new Vector3(180,4.3f,3),new Vector3(184,2.9f,2.4f),new Vector3(188,1.4f,2.4f)
 };
 public static void Apply()
 {
  var old=GameObject.Find("Diseño v5 · rutas de salto");if(old)Object.DestroyImmediate(old);
  foreach(var t in Object.FindObjectsByType<Transform>().ToArray())
   if(t&&(t.name=="Plataforma de madera"||t.name=="Borde claro"||t.name=="Base sólida · salto obligatorio"||t.name=="Pasarela diagonal de tejados"))Object.DestroyImmediate(t.gameObject);
  var root=new GameObject("Diseño v5 · rutas de salto").transform;
  for(int i=0;i<Platforms.Length;i++)
  {
   var p=Platforms[i];float floor=p.x>79&&p.x<90?-2:VillageTerrainProfile.Height(p.x)+.12f;
   var o=Box(root,"Plataforma de madera",new Vector3(p.x,p.y-.2f,0),new Vector3(p.z,.4f,2.5f),"Wood");o.AddComponent<VillageOneWayPlatform>();
   Box(root,"Borde claro",new Vector3(p.x,p.y+.025f,0),new Vector3(p.z,.05f,2.5f),p.x>79&&p.x<90&&p.y<1?"Stone":p.y-floor<2.65f?"Grass":"Wood",false);
   float bottom=p.y-.4f;
   if(p.x>79&&p.x<90&&p.y<1)Box(root,"Piedra estrecha del arroyo",new Vector3(p.x,(bottom+floor)*.5f,0),new Vector3(.65f,Mathf.Max(.1f,bottom-floor),1.8f),"Stone");
   else if(bottom-floor<2.25f)Box(root,"Terraplén de apoyo",new Vector3(p.x,(bottom+floor)*.5f,0),new Vector3(p.z,Mathf.Max(.1f,bottom-floor),2.5f),"Earth");
   else for(int side=-1;side<=1;side+=2)Box(root,"Puntal de pasarela",new Vector3(p.x+side*(p.z*.5f-.18f),(bottom+floor)*.5f,1.05f),new Vector3(.18f,bottom-floor,.18f),"Wood",false);
  }
  foreach(var c in Object.FindObjectsByType<VillageChest>())if(c.transform.position.x>60&&c.transform.position.x<70)c.transform.position=new Vector3(62,5.2f,0);
  foreach(var z in Object.FindObjectsByType<VillageZone>())
  {
   float x=z.transform.position.x;
   if(z.kind=="Hielo"&&x>59&&x<70)z.transform.position=new Vector3(58,4.8f,0);
   if(z.kind=="Armadura"&&x>30&&x<40)z.transform.position=new Vector3(35,4.9f,0);
   if(z.kind=="Fuego"&&x>145&&x<160)z.transform.position=new Vector3(157,6.2f,0);
   if(z.kind=="Salud"&&x>79&&x<94)z.transform.position=new Vector3(86.5f,4.8f,0);
   if(z.kind=="Salud"&&x>175&&x<183)z.transform.position=new Vector3(180,4.9f,0);
  }
  var archer=Object.FindObjectsByType<VillageEnemy>().Single(e=>e.ranged);archer.transform.position=new Vector3(43,7.1f,0);archer.detectionRange=12;
  var rock=GameObject.Find("Montaña del arquero · fondo");if(rock){rock.transform.position=new Vector3(43,2.4f,3.8f);rock.transform.localScale=new Vector3(5,8,2);}
  Box(root,"Cornisa del arquero · fondo",new Vector3(43,6,2.2f),new Vector3(1.5f,.4f,1),"Stone",false);
  foreach(var label in Object.FindObjectsByType<TextMesh>().ToArray())
  {
   if(label.text.StartsWith("04 · ARROYO")&&!label.name.StartsWith("RUTA BAJA:")){Object.DestroyImmediate(label.gameObject);continue;}
   if(new[]{"Hielo","Fuego","Salud","Armadura"}.Contains(label.text))
   {var nearest=Object.FindObjectsByType<VillageZone>().Where(z=>z.kind==label.text).OrderBy(z=>Mathf.Abs(z.transform.position.x-label.transform.position.x)).FirstOrDefault();if(nearest)label.transform.position=nearest.transform.position+new Vector3(0,1.1f,-1.8f);}
   if(label.text.StartsWith("RUTA BAJA:")||label.text.StartsWith("04 · ARROYO")){label.text="04 · ARROYO\nBAJO: piedras / ALTO: pasarela";label.transform.position=new Vector3(82,7.3f,-1.8f);}
   if(label.text.StartsWith("02 · ARQUERO")){label.text="02 · ARQUERO EN ALTURA\nFlechas hacia abajo · G + W";label.transform.position=new Vector3(43,9.5f,-1.8f);}
   if(label.text.StartsWith("COFRE")&&label.transform.position.x>60&&label.transform.position.x<70)label.transform.position=new Vector3(62,6.4f,-1.8f);
  }
 }
}
