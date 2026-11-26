using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
[InitializeOnLoad]
public static class VillageRevisionV7
{
 static double next;
 static readonly List<Vector3> points=new List<Vector3>();
 static VillageRevisionV7(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=OnPlay;}
 static void Poll()
 {
  if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;next=EditorApplication.timeSinceStartup+2;
  if(!File.Exists("Docs/village-v7.request"))return;var mode=File.ReadAllText("Docs/village-v7.request").Trim();File.Delete("Docs/village-v7.request");ApplyBoth();
  if(mode=="test"){EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");SessionState.SetBool("VillageV7Test",true);EditorApplication.isPlaying=true;}
 }
 static void OnPlay(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillageV7Test",false)){SessionState.SetBool("VillageV7Test",false);new GameObject("Verificación estrellas y recuperación").AddComponent<VillageV7Smoke>();}}
 public static void ApplyBoth()
 {
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  foreach(var path in new[]{"Assets/00_Scenes/Pueblo_01_Blockout.unity","Assets/00_Scenes/Pueblo_Online.unity"}){var scene=EditorSceneManager.OpenScene(path);Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  AssetDatabase.SaveAssets();File.WriteAllText("Docs/Verification/v7-layout.txt","Both scenes saved. Stars per player: "+points.Count+"\n"+System.DateTime.UtcNow.ToString("s"));
 }
 static Material Material(string name,Color color,bool transparent)
 {
  var path="Assets/03_Materials/Village_"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
  mat.color=color;mat.SetFloat("_Smoothness",.55f);
  if(transparent){mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.SetOverrideTag("RenderType","Transparent");mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;}
  else{mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(.65f,.35f,.02f));}
  EditorUtility.SetDirty(mat);return mat;
 }
 static Mesh StarMesh()
 {
  const string path="Assets/04_LevelGeometry/GuidingStar.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
  mesh=new Mesh{name="Estrella de cinco puntas"};var vertices=new List<Vector3>();var triangles=new List<int>();
  vertices.Add(new Vector3(0,0,-.075f));vertices.Add(new Vector3(0,0,.075f));
  for(int side=0;side<2;side++)for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;float r=i%2==0?.34f:.15f;vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,side==0?-.075f:.075f));}
  for(int i=0;i<10;i++){int n=(i+1)%10;triangles.AddRange(new[]{0,2+n,2+i,1,12+i,12+n,2+i,2+n,12+i,12+i,2+n,12+n});}
  mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static bool Solid(Collider c)=>!c.GetComponentInParent<VillageHero>()&&!c.GetComponentInParent<VillageEnemy>();
 static void Point(Vector3 p)
 {
  if(points.Any(x=>Vector3.Distance(x,p)<.65f))return;
  if(Physics.OverlapSphere(p,.24f,~0,QueryTriggerInteraction.Ignore).Any(Solid))return;
  points.Add(p);
 }
 static void GroundGroup(float from,float to)
 {for(int i=0;i<5;i++){float x=Mathf.Lerp(from,to,i/4f);Point(new Vector3(x,VillageTerrainProfile.Height(x)+1.17f,0));}}
 static void JumpPath(params Vector2[] path)
 {
  for(int i=0;i<path.Length-1;i++)
  {
   var a=path[i];var b=path[i+1];if(Mathf.Abs(b.x-a.x)>6)continue;
   var hero=Object.FindAnyObjectByType<VillageGame>().warrior;float speed=hero.jumpSpeed,gravity=hero.gravity;
   float discriminant=speed*speed-2*gravity*(b.y-a.y);if(discriminant<0)continue;
   float flight=(speed+Mathf.Sqrt(discriminant))/gravity;
   int count=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(b.x-a.x)/1.3f));
   for(int k=0;k<=count;k++){float t=k/(float)count,time=t*flight;Point(new Vector3(Mathf.Lerp(a.x,b.x,t),a.y+1.05f+speed*time-.5f*gravity*time*time,0));}
  }
 }
 public static void Apply()
 {
  var old=GameObject.Find("Revisión v7 · estrellas guía");if(old)Object.DestroyImmediate(old);var current=GameObject.Find("Estrellas · grupos compartidos");if(current)Object.DestroyImmediate(current);
  var root=new GameObject("Estrellas · grupos compartidos").transform;points.Clear();Physics.SyncTransforms();
  GroundGroup(3,7.8f);JumpPath(new Vector2(10,1.4f),new Vector2(14,2.8f));
  JumpPath(new Vector2(31,2.9f),new Vector2(35,4.3f));GroundGroup(39,43.8f);
  JumpPath(new Vector2(50,1.5f),new Vector2(54,2.9f));JumpPath(new Vector2(58,4.3f),new Vector2(62,4.3f));
  JumpPath(new Vector2(77,.12f),new Vector2(81,.6f));JumpPath(new Vector2(85,-.6f),new Vector2(89,.6f));
  JumpPath(new Vector2(78.5f,5.6f),new Vector2(82.5f,6.4f));
  GroundGroup(108,112.8f);GroundGroup(115,119.8f);
  JumpPath(new Vector2(123,1.4f),new Vector2(127,2.8f));GroundGroup(131.5f,136.3f);
  JumpPath(new Vector2(149,2.8f),new Vector2(153,4.2f));JumpPath(new Vector2(157,5.6f),new Vector2(161,4.2f));
  JumpPath(new Vector2(172,1.5f),new Vector2(176,2.9f));JumpPath(new Vector2(180,4.3f),new Vector2(184,2.9f));
  GroundGroup(191,194.6f);JumpPath(new Vector2(196,1.4f),new Vector2(200,2.8f));JumpPath(new Vector2(204,4.2f),new Vector2(208,2.8f));
  GroundGroup(214.4f,218.2f);JumpPath(new Vector2(224,2.8f),new Vector2(228,4.2f));JumpPath(new Vector2(232,2.8f),new Vector2(236,1.4f));
  if(points.Count>VillageStar.Capacity)throw new System.InvalidOperationException("Too many stars: "+points.Count);
  var mesh=StarMesh();var gold=Material("StarGold",new Color(1,.77f,.12f),false);
  for(int i=0;i<points.Count;i++)
  {
   var o=new GameObject("Estrella "+i.ToString("000"));o.transform.SetParent(root);o.transform.position=points[i];var trigger=o.AddComponent<SphereCollider>();trigger.radius=.36f;trigger.isTrigger=true;
   var v=new GameObject("Visual estrella");v.transform.SetParent(o.transform,false);v.AddComponent<MeshFilter>().sharedMesh=mesh;v.AddComponent<MeshRenderer>().sharedMaterial=gold;
   var star=o.AddComponent<VillageStar>();star.id=i;star.visual=v.transform;
  }
  foreach(var label in Object.FindObjectsByType<TextMesh>())if(label.text.Contains("Mantener juntos")||label.text.Contains("Guerrero en AZUL"))label.text="Guerrero: piso AZUL\nMago: piso MORADO\nPaso al estar ambos encima";
  var mage=Object.FindAnyObjectByType<VillageGame>().mage;mage.protectionMaterial=Material("ProtectionField",new Color(.28f,.75f,1,.16f),true);EditorUtility.SetDirty(mage);
 }
}
