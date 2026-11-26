using System;
using UnityEditor;
using UnityEngine;

internal static class VillageWarriorVisualSetup
{
    const string Bare = "Assets/05_Characters/WarriorPrototype/";
    const string Armor = "Assets/05_Characters/WarriorEquipped/";
    const string Path = "Assets/02_Prefabs/Resources/VillageWarriorVisual.prefab";

    [MenuItem("Battle in Kingdoms/Actualizar modelo del guerrero del pueblo")]
    internal static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Sal de Play Mode primero.");
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Bare + "GuerreroPrueba.controller");
        if (!controller) throw new InvalidOperationException("Falta el controlador del guerrero.");
        var root = new GameObject("Guerrero del pueblo · modelo");
        try
        {
            var visual = root.AddComponent<VillageWarriorVisual>();
            visual.bare = Attach(root.transform, Bare, "GuerreroSinEquipoJugable", "Guerrero_PielYRopa", controller);
            visual.equipped = Attach(root.transform, Armor, "GuerreroArmaduraJugable", "Guerrero_Original", controller);
            visual.equipped.gameObject.SetActive(false);
            if (!PrefabUtility.SaveAsPrefabAsset(root, Path)) throw new InvalidOperationException("No se guardó " + Path);
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static Animator Attach(Transform parent, string folder, string filename, string skin, RuntimeAnimatorController controller)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(folder + filename + ".fbx");
        if (!source) throw new InvalidOperationException("Falta " + filename);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        var animator = model.GetComponent<Animator>();
        if (!animator) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        WarriorPreviewSetup.SetMaterials(model, folder, skin);
        return animator;
    }
}
