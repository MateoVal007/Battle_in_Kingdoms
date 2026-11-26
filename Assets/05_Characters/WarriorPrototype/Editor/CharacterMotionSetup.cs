using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using static WarriorPreviewSetup;

public static class CharacterMotionSetup
{
    const string MageFolder = "Assets/05_Characters/MagePrototype/";
    const string WarriorFolder = "Assets/05_Characters/WarriorPrototype/";
    const string OriginalFolder = "Assets/05_Characters/WarriorEquipped/";

    [MenuItem("Battle in Kingdoms/Actualizar animaciones de personajes")]
    public static void Apply()
    {
        var active = EditorSceneManager.GetActiveScene();
        bool interruptedPreview = string.IsNullOrEmpty(active.path) && active.rootCount == 1
            && active.GetRootGameObjects()[0].name == "Guerrero equipado · pose original";
        if (EditorApplication.isPlayingOrWillChangePlaymode || (active.isDirty && !interruptedPreview))
            throw new InvalidOperationException("Guarda la escena y sal de Play Mode primero.");

        var warrior = WarriorMotions();
        var mage = MageMotions();
        UpdateController(WarriorFolder + "GuerreroPrueba.controller", warrior);
        UpdateController(MageFolder + "MagoPrueba.controller", mage);
        CreateOriginalPreview();
        AssetDatabase.SaveAssets();
        Debug.Log($"PERSONAJES_MOV_V2 · {warrior.Count} clips del guerrero y {mage.Count} clips del mago vinculados");
    }

    [MenuItem("Battle in Kingdoms/Actualizar prueba Mixamo del guerrero")]
    public static void ApplyWarrior()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Sal de Play Mode para actualizar el Animator.");

        WarriorPreviewSetup.Setup();
    }

    internal static Dictionary<string, AnimationClip> WarriorMotions()
    {
        var clips = ImportSet(WarriorFolder + "GuerreroSinEquipoJugable.fbx", true,
            "Idle", "Punch", "Ready", "Walk", "Run", "Prepare", "Jump", "Land", "HitBare", "CrouchDownBare", "CrouchIdleBare", "CrouchWalkBare", "CrouchPunch");
        var equipped = ImportSet(OriginalFolder + "GuerreroArmaduraJugable.fbx", true,
            "IdleEquipped", "ReadyEquipped", "WalkEquipped", "RunEquipped", "PrepareEquipped", "JumpEquipped", "LandEquipped",
            "CrouchDown", "CrouchIdle", "GuardRaise", "Guard", "GuardCrouch", "BlockImpact", "HitEquipped", "SwordSlash");
        foreach (var pair in equipped) clips.Add(pair.Key, pair.Value);
        return clips;
    }

    internal static Dictionary<string, AnimationClip> MageMotions() =>
        ImportSet(MageFolder + "MagoMovimientos.fbx", "Walk", "Prepare", "Jump", "Land");

    static string ActionName(string name) => name.Split('|').Last();

    static Dictionary<string, AnimationClip> ImportSet(string path, params string[] states) =>
        ImportSet(path, false, states);

    static Dictionary<string, AnimationClip> ImportSet(string path, bool generic, params string[] states)
    {
        bool Loops(string name)
        {
            string action = ActionName(name);
            return action.StartsWith("Run") || action.StartsWith("Walk") || action.StartsWith("Ready") || action.StartsWith("Idle") ||
                action.StartsWith("CrouchIdle") || action is "CrouchWalkBare" or "Guard" or "GuardCrouch";
        }

        // The equipped mesh has its own skeleton. Its clips use that exact hierarchy.
        if (generic) ImportGenericAtPath(path, Loops);
        else ImportHumanoidAtPath(path, Loops, true);
        var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
        return states.ToDictionary(name => name, name =>
        {
            var clip = clips.SingleOrDefault(item => ActionName(item.name) == name);
            if (!clip || clip.humanMotion == generic)
                throw new InvalidOperationException($"Clip ausente o incompatible: {name}. Importados: {string.Join(", ", clips.Select(x => x.name))}");
            return clip;
        });
    }

    internal static void UpdateController(string path, Dictionary<string, AnimationClip> clips)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (!controller) throw new InvalidOperationException("Animator ausente: " + path);
        var machine = controller.layers[0].stateMachine;
        foreach (var pair in clips)
        {
            var state = machine.states.Select(child => child.state).FirstOrDefault(item => item.name == pair.Key);
            if (!state) state = machine.AddState(pair.Key);
            state.motion = pair.Value;
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);
    }

    static void CreateOriginalPreview()
    {
        const string modelPath = OriginalFolder + "GuerreroEquipadoOriginal.fbx";
        const string scenePath = OriginalFolder + "Guerrero_Equipado_Prueba.unity";
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
        var clips = importer.defaultClipAnimations;
        foreach (var item in clips) { item.loopTime = true; item.loopPose = true; }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var idle = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .First(clip => !clip.name.StartsWith("__preview__"));
        var controller = BuildController(OriginalFolder + "IdleEquipado.controller",
            new Dictionary<string, AnimationClip> { { "Idle", idle } });
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var pivot = new GameObject("Guerrero equipado · pose original");
        pivot.transform.rotation = Quaternion.Euler(0, 152, 0);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        visual.transform.SetParent(pivot.transform, false);
        var animator = visual.GetComponent<Animator>();
        if (!animator) animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        SetMaterials(visual, OriginalFolder, "Guerrero_Original", "Mesh_0");
        var camera = CreateStage(OriginalFolder);
        camera.orthographicSize = 1.2f;
        camera.transform.LookAt(Vector3.up * .93f);
        EditorSceneManager.SaveScene(scene, scenePath);
    }
}
