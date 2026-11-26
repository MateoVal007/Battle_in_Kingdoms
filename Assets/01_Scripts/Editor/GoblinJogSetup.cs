using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.Collections;
using UnityEngine;
using Valeriano.CharacterPreview;

public static class GoblinJogSetup
{
    const string Art="Assets/05_Characters/Goblin/";
    const string Runtime="Assets/02_Prefabs/Resources/";
    [MenuItem("Battle in Kingdoms/Actualizar goblin y carrera equipada")]
    public static void Build()
    {
        BuildJog();BuildGoblin();PlaceGoblins();AssetDatabase.SaveAssets();
        Debug.Log("GOBLIN JOG BUILD PASS: 3 goblins in the middle of each Pueblo scene, survival wave composition, equipped Jog_Fwd loop");
    }
    static void BuildJog()
    {
        const string library="Assets/05_Characters/WarriorHumanoidTrial/UAL1_Standard.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(library);var clips=importer.clipAnimations;
        if(clips.Length==0)clips=importer.defaultClipAnimations;
        var take=clips.Single(c=>c.name.Split('|').Last()=="Jog_Fwd_Loop");
        take.loopTime=take.loopPose=true;take.lockRootRotation=take.lockRootHeightY=take.lockRootPositionXZ=true;
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var clip=AssetDatabase.LoadAllAssetsAtPath(library).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")&&c.name.Split('|').Last()=="Jog_Fwd_Loop");
        if(!clip.humanMotion||!clip.isLooping)throw new Exception("Jog_Fwd must be a looping Humanoid clip");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/05_Characters/WarriorHumanoidTrial/SwordAttack.controller");
        State(controller,"JogEquipped",clip,1f);
        var root=PrefabUtility.LoadPrefabContents(Runtime+"VillageWarriorVisual.prefab");
        try
        {
            var view=root.GetComponent<VillageWarriorVisual>();
            var shield=view.libraryAttack.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name.StartsWith("Escudo"));
            view.joggingShieldNormal=ShieldNormal(shield.GetComponent<MeshFilter>().sharedMesh);
            PrefabUtility.SaveAsPrefabAsset(root,Runtime+"VillageWarriorVisual.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static Vector3 ShieldNormal(Mesh mesh)
    {
        // Editor-only access also works for meshes whose CPU data is stripped in a build.
        using var data=MeshUtility.AcquireReadOnlyMeshData(mesh);
        using var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp);
        using var normals=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp);
        data[0].GetVertices(vertices);data[0].GetNormals(normals);
        Vector3 center=Vector3.zero,hint=Vector3.zero;
        foreach(var v in vertices)center+=v;center/=vertices.Length;
        foreach(var n in normals)hint+=n;
        var cov=new double[3,3];var basis=new double[3,3];for(int i=0;i<3;i++)basis[i,i]=1;
        foreach(var v in vertices){var d=v-center;for(int i=0;i<3;i++)for(int j=0;j<3;j++)cov[i,j]+=d[i]*d[j];}
        for(int step=0;step<24;step++)
        {
            int p=0,q=1;for(int i=0;i<3;i++)for(int j=i+1;j<3;j++)if(Math.Abs(cov[i,j])>Math.Abs(cov[p,q])){p=i;q=j;}
            if(Math.Abs(cov[p,q])<1e-22)break;
            double tau=(cov[q,q]-cov[p,p])/(2*cov[p,q]);double t=(tau>=0?1:-1)/(Math.Abs(tau)+Math.Sqrt(1+tau*tau));double c=1/Math.Sqrt(1+t*t),s=t*c,off=cov[p,q];
            cov[p,p]-=t*off;cov[q,q]+=t*off;cov[p,q]=cov[q,p]=0;
            for(int k=0;k<3;k++)if(k!=p&&k!=q){double a=cov[k,p],b=cov[k,q];cov[k,p]=cov[p,k]=c*a-s*b;cov[k,q]=cov[q,k]=s*a+c*b;}
            for(int k=0;k<3;k++){double a=basis[k,p],b=basis[k,q];basis[k,p]=c*a-s*b;basis[k,q]=s*a+c*b;}
        }
        int axis=0;for(int i=1;i<3;i++)if(cov[i,i]<cov[axis,axis])axis=i;
        var normal=new Vector3((float)basis[0,axis],(float)basis[1,axis],(float)basis[2,axis]).normalized;
        if(Vector3.Dot(normal,hint)<0)normal=-normal;
        return normal;
    }
    internal static void BuildGoblin()
    {
        GameplayPresentationSetup.SurfacesFor(Art,"Goblin_Animado");
        var source=WarriorPreviewSetup.ImportGenericAtPath(Art+"Goblin_Animado.fbx",n=>n.EndsWith("GoblinWalk")||n.EndsWith("GoblinIdle"));
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Art+"Goblin.controller")??AnimatorController.CreateAnimatorControllerAtPath(Art+"Goblin.controller");
        var clips=AssetDatabase.LoadAllAssetsAtPath(Art+"Goblin_Animado.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        foreach(var pair in new[]{new[]{"Idle","GoblinIdle"},new[]{"Walk","GoblinWalk"},new[]{"Attack","GoblinAttack"},new[]{"Hit","GoblinHit"},new[]{"Fall","GoblinFall"}})
            State(controller,pair[0],clips.Single(c=>c.name.Split('|').Last()==pair[1]),1);
        var root=new GameObject("Goblin · cuchillo");var view=root.AddComponent<VillageEnemyVisual>();
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);view.animator=model.GetComponent<Animator>();view.animator.runtimeAnimatorController=controller;view.animator.applyRootMotion=false;view.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var surface=AssetDatabase.FindAssets("t:Material",new[]{Art.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".mat")).Select(AssetDatabase.LoadAssetAtPath<Material>).Single();
        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMaterial=surface;
        var height=model.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y)-model.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);
        if(height<1.4f||height>1.8f)throw new Exception("Unexpected goblin scale: "+height);
        model.transform.localScale=Vector3.one*(1.9f/height);
        PrefabUtility.SaveAsPrefabAsset(root,Runtime+"VillageGoblinVisual.prefab");UnityEngine.Object.DestroyImmediate(root);
        if(!AssetDatabase.IsValidFolder("Assets/02_Prefabs/Enemies"))AssetDatabase.CreateFolder("Assets/02_Prefabs","Enemies");
        root=GameObject.CreatePrimitive(PrimitiveType.Capsule);root.name="Goblin · perseguidor con cuchillo";root.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_Danger.mat");
        var enemy=root.AddComponent<VillageEnemy>();enemy.goblin=true;enemy.health=46;enemy.damage=10;enemy.moveSpeed=2.8f;enemy.detectionRange=12;enemy.attackRange=1.75f;enemy.windup=.45f;enemy.cooldown=1.25f;enemy.patrolRadius=1.6f;enemy.leashRadius=12;
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/02_Prefabs/Enemies/Goblin_Plataformas.prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static void PlaceGoblins()
    {
        foreach(string name in new[]{"Pueblo_01_Blockout","Pueblo_Online"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/"+name+".unity");
            if(UnityEngine.Object.FindObjectsByType<VillageEnemy>().Any(e=>e.goblin))continue;
            var game=UnityEngine.Object.FindAnyObjectByType<VillageGame>();Physics.SyncTransforms();
            var holder=new GameObject("Goblins · plaza y molino");
            foreach(float x in new[]{124f,139f,148f})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Enemies/Goblin_Plataformas.prefab"),holder.transform);
                go.name="Goblin · zona central · "+x;go.transform.position=game.SafeSpawn(x,3.5f)-Vector3.up*.25f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            }
            if(UnityEngine.Object.FindObjectsByType<VillageEnemy>().Length>64)throw new Exception("Enemy snapshot capacity exceeded");
            EditorSceneManager.SaveScene(scene);
        }
    }
    static void State(AnimatorController controller,string name,AnimationClip clip,float speed)
    {
        var machine=controller.layers[0].stateMachine;var state=machine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??machine.AddState(name);state.motion=clip;state.speed=speed;
        if(name=="Idle")machine.defaultState=state;EditorUtility.SetDirty(state);
    }
}
