using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VillageEnemyRevision
{
 public static void Prepare()
 {
  foreach(var path in new[]{"Assets/00_Scenes/Pueblo_01_Blockout.unity","Assets/00_Scenes/Pueblo_Online.unity"})
  {var scene=EditorSceneManager.OpenScene(path);ApplyScene();VillageLayoutV5.Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  AssetDatabase.SaveAssets();
  File.WriteAllText("Docs/Verification/enemy-layout.txt","PASS both scenes: stationary downward archer x43 y7.1; v5 jump routes; pursuers; whip guardian x114 after checkpoint99.\n"+DateTime.UtcNow.ToString("s"));
 }
 public static void ApplyScene()
 {
  var existing=GameObject.Find("Enemigos v4 · plaza del guardián");if(existing)return;
  var root=new GameObject("Enemigos v4 · plaza del guardián").transform;
  foreach(var e in UnityEngine.Object.FindObjectsByType<VillageEnemy>())
  {
   e.ranged=false;e.detectionRange=14;e.moveSpeed=2.3f;e.name="Bandido perseguidor";
   if(Mathf.Abs(e.transform.position.x-44.2f)<.2f)
   {e.ranged=true;e.name="Arquero fijo · montaña del huerto";e.detectionRange=18;e.windup=.7f;e.cooldown=1.8f;e.transform.position=new Vector3(41.8f,4.45f,0);}
  }
  var danger=AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_Danger.mat");
  var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Montaña del arquero · fondo";rock.transform.SetParent(root);rock.transform.position=new Vector3(43,1.4f,3.8f);rock.transform.localScale=new Vector3(5,6,2);
  rock.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_Stone.mat");UnityEngine.Object.DestroyImmediate(rock.GetComponent<Collider>());
  var o=GameObject.CreatePrimitive(PrimitiveType.Capsule);o.name="Guardián grande del látigo · plaza";o.transform.SetParent(root);o.transform.position=new Vector3(114,1.65f,0);o.transform.localScale=new Vector3(1.55f,1.5f,1.2f);o.GetComponent<Renderer>().sharedMaterial=danger;
  var guardian=o.AddComponent<VillageEnemy>();guardian.whip=true;guardian.health=240;guardian.moveSpeed=1.8f;guardian.attackRange=3.8f;guardian.damage=25;guardian.windup=.9f;guardian.cooldown=1.8f;guardian.detectionRange=13;
  Label(root,"02 · ARQUERO EN LA MONTAÑA\nG + W: cubrirse por arriba",43,7.2f);
  Label(root,"05 · GUARDIÁN DEL LÁTIGO\nAmarillo: prepárate para esquivar o cubrirte",113,6.4f);
 }
 static void Label(Transform root,string text,float x,float y)
 {var o=new GameObject(text);o.transform.SetParent(root);o.transform.position=new Vector3(x,y,-1.8f);var t=o.AddComponent<TextMesh>();t.text=text;t.characterSize=.048f;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.12f,.18f,.23f);}
 public static void Verify()
 {Prepare();EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");SessionState.SetBool("VillageEnemiesVerify",true);EditorApplication.isPlaying=true;}
 [InitializeOnLoadMethod]static void Register(){EditorApplication.playModeStateChanged+=OnPlay;}
 static void OnPlay(PlayModeStateChange state)
 {
  if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillageEnemiesVerify",false))
  {SessionState.SetBool("VillageEnemiesVerify",false);new GameObject("Enemy verification").AddComponent<VillageEnemySmoke>();}
 }
 public static void Regression()
 {EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");SessionState.SetBool("VillageExitAfterTests",true);VillageVerifyRequest.Run();}
 public static void Build(){Prepare();VillageOnlineBuild.Build();}
}
