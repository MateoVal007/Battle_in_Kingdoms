using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class VillageMageVisualSetup
{
    const string Folder = "Assets/05_Characters/MagePrototype/";
    const string Model = Folder + "MagoPueblo.fbx";
    const string Prefab = "Assets/02_Prefabs/Resources/VillageMageVisual.prefab";

    [MenuItem("Battle in Kingdoms/Actualizar modelo del mago del pueblo")]
    internal static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Sal de Play Mode primero.");
        string ClipName(string name) => name.Split('|').Last();
        var model = WarriorPreviewSetup.ImportGenericAtPath(Model, name => ClipName(name) is "Idle" or "Walk" or "CrouchIdle");
        var available = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
        var clips = new[] { "Idle", "Walk", "CrouchDown", "CrouchIdle", "Jump", "Land", "Cast", "CrouchCast", "AirCast" }
            .ToDictionary(name => name, name => available.Single(clip => ClipName(clip.name) == name));
        var controller = WarriorPreviewSetup.BuildController(Folder + "MagoPueblo.controller", clips);
        var root = new GameObject("Mago del pueblo · modelo");
        try
        {
            var visual = root.AddComponent<VillageMageVisual>();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            visual.animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            visual.animator.runtimeAnimatorController = controller;
            visual.animator.applyRootMotion = false;
            WarriorPreviewSetup.SetMaterials(instance, Folder, "Mago_Tunica", "Baston");
            if (!PrefabUtility.SaveAsPrefabAsset(root, Prefab)) throw new InvalidOperationException("No se guardó " + Prefab);
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}

