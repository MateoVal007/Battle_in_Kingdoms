using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Fusion;

[InitializeOnLoad]
public static class VillageOnlineBuild
{
    const string ScenePath="Assets/00_Scenes/Pueblo_Online.unity";
    const string Output="C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/Pueblo_online";
    static double next;
    static VillageOnlineBuild(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;next=EditorApplication.timeSinceStartup+2;
        if(File.Exists("Docs/village-local-regression.request")){File.Delete("Docs/village-local-regression.request");EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");VillageVerifyRequest.Run();return;}
        if(File.Exists("Docs/village-online-open.request")){File.Delete("Docs/village-online-open.request");EditorSceneManager.OpenScene(ScenePath);return;}
        if(File.Exists("Docs/village-editor-host.request")){File.Delete("Docs/village-editor-host.request");EditorSceneManager.OpenScene(ScenePath);SessionState.SetBool("VillageOnlineEditorTest",true);EditorApplication.isPlaying=true;return;}
        if(File.Exists("Docs/village-online-build.request")){File.Delete("Docs/village-online-build.request");Build();}}
    [MenuItem("Battle in Kingdoms/Preparar y compilar prueba online")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        try
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))
            {
                var source=EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");
                new GameObject("Menú online").AddComponent<VillageOnline>();
                var world=new GameObject("Estado del pueblo online");world.AddComponent<NetworkObject>();world.AddComponent<VillageNetworkWorld>();
                EditorSceneManager.SaveScene(source,ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var root in scene.GetRootGameObjects())new NetworkObjectBaker().Bake(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Output+"/Pueblo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Output+"/build-result.txt",report.summary.result+" · errors="+report.summary.totalErrors+" · "+System.DateTime.Now.ToString("s")+"\n"+string.Join("\n",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==UnityEngine.LogType.Error||m.type==UnityEngine.LogType.Exception).Select(m=>m.content)));
            Debug.Log("PUEBLO ONLINE BUILD: "+report.summary.result);
        }
        catch(System.Exception e){File.WriteAllText(Output+"/build-result.txt",e.ToString());Debug.LogException(e);}
    }
}

