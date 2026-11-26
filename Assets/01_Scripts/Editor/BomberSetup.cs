using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valeriano.CharacterPreview;

public static class BomberSetup
{
    const string Art="Assets/05_Characters/Bomber/";
    [MenuItem("Battle in Kingdoms/Actualizar lanzabombas y tamaño del duende")]
    public static void Build()
    {
        GoblinJogSetup.BuildGoblin();
        GameplayPresentationSetup.SurfacesFor(Art,"Bomber_Animado");
        var source=WarriorPreviewSetup.ImportGenericAtPath(Art+"Bomber_Animado.fbx",n=>n.EndsWith("BomberWalk")||n.EndsWith("BomberWalkBack")||n.EndsWith("BomberIdle"));
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Art+"Bomber.controller")??AnimatorController.CreateAnimatorControllerAtPath(Art+"Bomber.controller");
        var clips=AssetDatabase.LoadAllAssetsAtPath(Art+"Bomber_Animado.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var machine=controller.layers[0].stateMachine;
        foreach(string name in new[]{"Idle","Walk","WalkBack","Attack","Hit","Fall"})
        {
            var state=machine.states.Select(c=>c.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);
            state.motion=clips.Single(c=>c.name.Split('|').Last()=="Bomber"+name);state.speed=1;EditorUtility.SetDirty(state);if(name=="Idle")machine.defaultState=state;
        }
        var root=new GameObject("Bomber · retirada y lanzamiento");var view=root.AddComponent<VillageEnemyVisual>();
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);view.animator=model.GetComponent<Animator>();view.animator.runtimeAnimatorController=controller;view.animator.applyRootMotion=false;view.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var material=AssetDatabase.FindAssets("t:Material",new[]{Art.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".mat")).Select(AssetDatabase.LoadAssetAtPath<Material>).Single();
        foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.sharedMaterial=material;
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/02_Prefabs/Resources/VillageBomberVisual.prefab");UnityEngine.Object.DestroyImmediate(root);
        root=GameObject.CreatePrimitive(PrimitiveType.Capsule);root.name="Bomber · mantiene distancia";
        root.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_Danger.mat");
        var enemy=root.AddComponent<VillageEnemy>();enemy.bomber=true;enemy.health=55;enemy.damage=12;enemy.moveSpeed=2.5f;enemy.detectionRange=15;enemy.attackRange=11;enemy.windup=.65f;enemy.cooldown=2.1f;enemy.patrolRadius=2;enemy.leashRadius=12;
        const string prefab="Assets/02_Prefabs/Enemies/Bomber_Plataformas.prefab";
        PrefabUtility.SaveAsPrefabAsset(root,prefab);UnityEngine.Object.DestroyImmediate(root);
        foreach(string name in new[]{"Pueblo_01_Blockout","Pueblo_Online"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/"+name+".unity");
            if(UnityEngine.Object.FindObjectsByType<VillageEnemy>().Any(e=>e.bomber))continue;
            var game=UnityEngine.Object.FindAnyObjectByType<VillageGame>();Physics.SyncTransforms();var holder=new GameObject("Lanzabombas · huertos y bosque");
            foreach(float x in new[]{36f,176f})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),holder.transform);go.name="Bomber · "+x;go.transform.position=game.SafeSpawn(x,3.5f)-Vector3.up*.25f;PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            }
            int entries=UnityEngine.Object.FindObjectsByType<VillageEnemy>().Length+UnityEngine.Object.FindObjectsByType<VillageChest>().Length+UnityEngine.Object.FindObjectsByType<VillageZone>().Count(z=>!z.spawnedByChest);
            if(entries>64)throw new Exception("Pueblo network capacity exceeded: "+entries);
            EditorSceneManager.SaveScene(scene);Debug.Log(name+": "+entries+" world entries, 2 bombers");
        }
        AssetDatabase.SaveAssets();Debug.Log("BOMBER BUILD PASS: prefabs, both Pueblo scenes, survival composition, larger goblin");
    }
}
