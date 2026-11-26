using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class VillageRevisionV8
{
 static double next;
 static VillageRevisionV8(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=OnPlay;}
 static void Poll()
 {
  if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;next=EditorApplication.timeSinceStartup+2;
  if(!File.Exists("Docs/village-v8.request"))return;var mode=File.ReadAllText("Docs/village-v8.request").Trim();File.Delete("Docs/village-v8.request");ApplyBoth();
  if(mode=="test"){EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");SessionState.SetBool("VillageV8Test",true);EditorApplication.isPlaying=true;}
 }
 static void OnPlay(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillageV8Test",false)){SessionState.SetBool("VillageV8Test",false);new GameObject("Verificación correcciones v8").AddComponent<VillageV8Smoke>();}}
 public static void ApplyBoth()
 {
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  foreach(var path in new[]{"Assets/00_Scenes/Pueblo_01_Blockout.unity","Assets/00_Scenes/Pueblo_Online.unity"}){var scene=EditorSceneManager.OpenScene(path);VillageRevisionV7.Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  AssetDatabase.SaveAssets();File.WriteAllText("Docs/Verification/v8-layout.txt","Both scenes saved. Shared stars: "+Object.FindObjectsByType<VillageStar>().Length+"\n"+System.DateTime.UtcNow.ToString("s"));
 }
}
