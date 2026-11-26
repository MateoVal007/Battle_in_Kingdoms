using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valeriano.CharacterPreview;

public static class BomberEnemy1Setup
{
    const string OriginalArt = "Assets/05_Characters/Bomber/";
    const string AltArt = "Assets/05_Characters/BomberEnemy1/";
    const string AltModelName = "Bomber_Enemy1";

    const string OriginalVisualPrefab = "Assets/02_Prefabs/Resources/VillageBomberVisual.prefab";
    const string AltVisualPrefab = "Assets/02_Prefabs/Resources/VillageBomber1Visual.prefab";
    const string AltEnemyPrefab = "Assets/02_Prefabs/Enemies/BomberEnemy1_Plataformas.prefab";
    const string HolderName = "Lanzabombas · Bomber_Enemy1";

    // Sectors where the original girl bomber is NOT placed (she stays at x=36 Huertos and x=176 Bosque):
    // x=58 -> Sector 03 Tejados (calle inferior), x=208 -> Sector 09 Camino Real
    static readonly float[] AltSpawnXs = { 58f, 208f };

    static readonly string[] TargetScenes =
    {
        "Assets/00_Scenes/Pueblo_01_Blockout.unity",
        "Assets/00_Scenes/Pueblo_Online.unity",
    };

    [MenuItem("Battle in Kingdoms/Agregar Bomber_Enemy1 (mantener chica Bomber)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Sal de Play Mode antes de agregar Bomber_Enemy1.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();

        // 1. Restore VillageBomberVisual.prefab to the original girl Bomber (Assets/05_Characters/Bomber/)
        RestoreOriginalGirlBomber();

        // 2. Build separate VillageBomber1Visual.prefab + BomberEnemy1_Plataformas.prefab for Bomber_Enemy1
        BuildAltBomberVisual();
        var enemyPrefab = BuildAltBomberEnemyPrefab();

        AssetDatabase.SaveAssets();

        // 3. Add Bomber_Enemy1 into other sectors of both Pueblo local and Pueblo online (keeping the girl bomber untouched!)
        string activeScene = EditorSceneManager.GetActiveScene().path;
        try
        {
            foreach (string scenePath in TargetScenes)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning("Escena no encontrada: " + scenePath);
                    continue;
                }

                AddAltBombersToScene(scenePath, enemyPrefab);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScene) && File.Exists(activeScene))
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("BOMBER_ENEMY1_ADD_PASS: chica Bomber restaurada y Bomber_Enemy1 agregado en Tejados (x=58) y Camino Real (x=208)");
    }

    static void RestoreOriginalGirlBomber()
    {
        GameplayPresentationSetup.SurfacesFor(OriginalArt, "Bomber_Animado");
        string fbxPath = OriginalArt + "Bomber_Animado.fbx";
        var source = WarriorPreviewSetup.ImportGenericAtPath(
            fbxPath,
            n => n.EndsWith("BomberWalk") || n.EndsWith("BomberWalkBack") || n.EndsWith("BomberIdle")
        );
        var controller = BuildController(OriginalArt + "Bomber.controller", fbxPath);

        var root = new GameObject("Bomber · retirada y lanzamiento");
        try
        {
            var view = root.AddComponent<VillageEnemyVisual>();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            view.animator = model.GetComponent<Animator>();
            view.animator.runtimeAnimatorController = controller;
            view.animator.applyRootMotion = false;
            view.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var mat = AssetDatabase.FindAssets("t:Material", new[] { OriginalArt.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".mat"))
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .FirstOrDefault();
            if (mat)
            {
                foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    smr.sharedMaterial = mat;
            }

            PrefabUtility.SaveAsPrefabAsset(root, OriginalVisualPrefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void BuildAltBomberVisual()
    {
        GameplayPresentationSetup.SurfacesFor(AltArt, AltModelName);
        string fbxPath = AltArt + AltModelName + ".fbx";
        var source = WarriorPreviewSetup.ImportGenericAtPath(
            fbxPath,
            n => n.EndsWith("BomberIdle") || n.EndsWith("BomberWalk") || n.EndsWith("BomberWalkBack")
        );
        var controller = BuildController(AltArt + "BomberEnemy1.controller", fbxPath);

        var root = new GameObject("Bomber_Enemy1 · retirada y lanzamiento");
        try
        {
            var view = root.AddComponent<VillageEnemyVisual>();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            view.animator = model.GetComponent<Animator>();
            view.animator.runtimeAnimatorController = controller;
            view.animator.applyRootMotion = false;
            view.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(AltArt + "Bomber_Enemy1_Mat.mat");
            if (mat)
            {
                foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    smr.sharedMaterial = mat;
            }

            PrefabUtility.SaveAsPrefabAsset(root, AltVisualPrefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static GameObject BuildAltBomberEnemyPrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        try
        {
            root.name = "Bomber_Enemy1 · mantiene distancia";
            var dangerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_Danger.mat");
            if (dangerMat) root.GetComponent<Renderer>().sharedMaterial = dangerMat;

            var enemy = root.AddComponent<VillageEnemy>();
            enemy.bomber = true;
            enemy.bomberAlt = true;
            enemy.health = 55;
            enemy.damage = 12;
            enemy.moveSpeed = 2.5f;
            enemy.detectionRange = 15;
            enemy.attackRange = 11;
            enemy.windup = 0.65f;
            enemy.cooldown = 2.1f;
            enemy.patrolRadius = 2;
            enemy.leashRadius = 12;

            return PrefabUtility.SaveAsPrefabAsset(root, AltEnemyPrefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static AnimatorController BuildController(string controllerPath, string fbxPath)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
            ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        var machine = controller.layers[0].stateMachine;
        foreach (string stateName in new[] { "Idle", "Walk", "WalkBack", "Attack", "Hit", "Fall" })
        {
            var clip = clips.FirstOrDefault(c => c.name.Split('|').Last() == "Bomber" + stateName)
                ?? clips.FirstOrDefault(c => c.name.EndsWith("Bomber" + stateName));
            if (!clip)
                throw new Exception($"No se encontró clip Bomber{stateName} en {fbxPath}");

            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName)
                ?? machine.AddState(stateName);
            state.motion = clip;
            state.speed = 1f;
            EditorUtility.SetDirty(state);
            if (stateName == "Idle") machine.defaultState = state;
        }
        return controller;
    }

    static void AddAltBombersToScene(string scenePath, GameObject enemyPrefab)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Idempotent: remove previous Bomber_Enemy1 group if re-running
        var existingHolder = GameObject.Find(HolderName);
        if (existingHolder) UnityEngine.Object.DestroyImmediate(existingHolder);

        var game = UnityEngine.Object.FindAnyObjectByType<VillageGame>();
        Physics.SyncTransforms();

        var holder = new GameObject(HolderName).transform;
        foreach (float x in AltSpawnXs)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, holder);
            go.name = "Bomber_Enemy1 · " + x;
            go.transform.position = game ? game.SafeSpawn(x, 3.5f) - Vector3.up * 0.25f : new Vector3(x, 1.0f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }

        int entries = UnityEngine.Object.FindObjectsByType<VillageEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length
            + UnityEngine.Object.FindObjectsByType<VillageChest>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length
            + UnityEngine.Object.FindObjectsByType<VillageZone>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(z => !z.spawnedByChest);

        if (entries > 64)
            throw new Exception($"Pueblo network capacity exceeded ({entries} > 64) in {scenePath}");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{scenePath}: {entries}/64 world entries (chica Bomber conservada + {AltSpawnXs.Length} Bomber_Enemy1 agregados)");
    }
}
