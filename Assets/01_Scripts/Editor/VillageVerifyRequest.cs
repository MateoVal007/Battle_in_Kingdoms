using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class VillageVerifyRequest {
 static VillageVerifyRequest(){EditorApplication.playModeStateChanged+=OnPlay;EditorApplication.delayCall+=Pending;EditorApplication.update+=Poll;}
 static double nextPoll;
 static void Poll(){if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;nextPoll=EditorApplication.timeSinceStartup+1;Pending();}
 static void Pending(){if(File.Exists("Docs/village-dressing.request")){File.Delete("Docs/village-dressing.request");VillageBuilder.Build();Run();return;}if(File.Exists("Docs/village-final.request")){File.Delete("Docs/village-final.request");Run();}}
 public static void CommandLine(){SessionState.SetBool("VillageExitAfterTests",true);VillageBuilder.Build();Run();}
 [MenuItem("Battle in Kingdoms/Verificar pueblo en Play Mode")]
 public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;if(SceneManager.GetActiveScene().name!="Pueblo_01_Blockout"){Debug.LogWarning("Abrir Pueblo_01_Blockout antes de verificar.");return;}SessionState.SetBool("VillageVerify",true);EditorApplication.isPlaying=true;}
 static void OnPlay(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("VillageExitAfterTests",false)){SessionState.SetBool("VillageExitAfterTests",false);EditorApplication.Exit(0);return;}if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillageVerify",false)){SessionState.SetBool("VillageVerify",false);new GameObject("Automated verification").AddComponent<VillageSmoke>();}}
}




