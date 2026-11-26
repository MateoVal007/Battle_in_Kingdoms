using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Valeriano.CharacterPreview;

public static class GameplayPresentationSetup
{
    public const string Prefabs = "Assets/02_Prefabs/Resources/";
    const string Warrior = "Assets/05_Characters/WarriorHumanoidTrial/";
    [Serializable] class Surface { public string name, @base, normal; public float[] color; public float metal; }
    [Serializable] class Surfaces { public Surface[] surfaces; }

    [MenuItem("Battle in Kingdoms/Actualizar poción arquero y movimientos")]
    public static void Build()
    {
        Ensure("Assets/02_Prefabs/Resources");
        foreach (string folder in new[] { "VillageProps", "VillageHazards", "LevelModels" })
            Move("Assets/Resources/" + folder, Prefabs + folder);
        foreach (string name in new[] { "VillageWarriorVisual", "VillageMageVisual", "VillageEnemy1Visual", "VillageDaggerVisual" })
            Move("Assets/Resources/" + name + ".prefab", Prefabs + name + ".prefab");
        BuildPotion(); BuildArcher(); BuildWarrior();
        AssetDatabase.SaveAssets();
        Debug.Log("PRESENTATION BUILD PASS potion, archer, Punch_Jab and Idle_Shield_Break; runtime prefabs organized with preserved GUIDs");
    }
    static void Ensure(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Ensure(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    static void Move(string from, string to)
    {
        if (!AssetDatabase.LoadMainAssetAtPath(from)) return;
        if (AssetDatabase.LoadMainAssetAtPath(to)) throw new Exception("Destino ya existe: " + to);
        string error = AssetDatabase.MoveAsset(from, to); if (!string.IsNullOrEmpty(error)) throw new Exception(error);
    }
    internal static void SurfacesFor(string folder, string model)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(folder + model + ".fbx");
        importer.importCameras = importer.importLights = false; importer.isReadable = true;
        foreach (var s in JsonUtility.FromJson<Surfaces>(File.ReadAllText(folder + "surface.json")).surfaces)
        {
            string safe = string.Concat(s.name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            string path = folder + safe + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", new Color(s.color[0], s.color[1], s.color[2], s.color[3]));
            mat.SetFloat("_Metallic", s.metal); mat.SetFloat("_Smoothness", .27f);
            foreach (var entry in new[] { new[] { s.@base, "_BaseMap" }, new[] { s.normal, "_BumpMap" } })
            {
                if (string.IsNullOrEmpty(entry[0])) continue;
                string texturePath = folder + entry[0]; var ti = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.CompressedHQ;
                if (entry[1] == "_BumpMap") ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport(); mat.SetTexture(entry[1], AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            if (!string.IsNullOrEmpty(s.normal)) { mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", .65f); }
            EditorUtility.SetDirty(mat); importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), s.name), mat);
        }
        importer.SaveAndReimport();
    }
    static void BuildPotion()
    {
        const string folder = "Assets/07_Models/GameplayProps/PocionSalud/";
        SurfacesFor(folder, "PocionSalud");
        var root = new GameObject("PocionSalud");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "PocionSalud.fbx"), root.transform);
        var b = Bounds(model); model.transform.localScale *= .8f / b.size.y; b = Bounds(model); model.transform.localPosition -= b.center;
        PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "VillageProps/PocionSalud.prefab"); UnityEngine.Object.DestroyImmediate(root);
    }
    static void BuildArcher()
    {
        const string folder = "Assets/05_Characters/Arquer/";
        SurfacesFor(folder, "Arquer_Reparado");
        WarriorPreviewSetup.ImportGenericAtPath(folder + "Arquer_Reparado.fbx", n => n.EndsWith("ArquerIdle"));
        var controller = Controller(folder + "Arquer.controller");
        foreach (var pair in new[] { new[] { "Idle", "ArquerIdle" }, new[] { "Shoot", "ArquerShoot" }, new[] { "Hit", "ArquerHit" }, new[] { "Fall", "ArquerFall" } })
            State(controller, pair[0], Clip(folder + "Arquer_Reparado.fbx", pair[1]), 1);
        var root = new GameObject("Arquero · ballesta reparada"); var visual = root.AddComponent<VillageArcherVisual>();
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Arquer_Reparado.fbx"), root.transform);
        visual.animator = model.GetComponent<Animator>(); visual.animator.runtimeAnimatorController = controller; visual.animator.applyRootMotion = false;
        visual.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var muzzle = new GameObject("Salida de virote"); muzzle.transform.SetParent(model.GetComponentsInChildren<Transform>().Single(t => t.name == "Chest"), false);
        muzzle.transform.position = new Vector3(.102f, .505f, .455f) * (1.9f / .9193041f); visual.muzzle = muzzle.transform;
        PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "VillageArcherVisual.prefab"); UnityEngine.Object.DestroyImmediate(root);
    }
    static void BuildWarrior()
    {
        string lib2 = "Assets/06_Animations/Quaternius2/UAL2_Standard.fbx";
        var source = (ModelImporter)AssetImporter.GetAtPath(lib2); source.bakeAxisConversion = true;
        WarriorPreviewSetup.ImportHumanoidAtPath(lib2, n => false);
        var breakClip = Clip(lib2, "Idle_Shield_Break"); var punch = Clip(Warrior + "UAL1_Standard.fbx", "Punch_Jab");
        if (!breakClip.humanMotion || !punch.humanMotion) throw new Exception("Los clips deben ser Humanoid");
        var bareSource = WarriorPreviewSetup.ImportHumanoidAtPath(Warrior + "GuerreroBareHumanoid.fbx", n => false, true);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(Warrior + "GuerreroBareHumanoid.fbx").OfType<Avatar>().Single();
        if (!avatar.isHuman || !avatar.isValid) throw new Exception("Avatar sin equipo inválido");
        string path = Prefabs + "VillageWarriorVisual.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var v = root.GetComponent<VillageWarriorVisual>();
            v.libraryAttack.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            v.libraryAttack.enabled = true;
            var controller = (AnimatorController)v.libraryAttack.runtimeAnimatorController;
            State(controller, "ShieldBreak", breakClip, breakClip.length / v.armorBreakDuration);
            if (v.libraryBare) UnityEngine.Object.DestroyImmediate(v.libraryBare.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(bareSource, root.transform); model.name = "Guerrero · Punch Jab";
            v.libraryBare = model.GetComponent<Animator>(); v.libraryBare.avatar = avatar; v.libraryBare.applyRootMotion = false;
            v.libraryBare.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var bareController = Controller(Warrior + "PunchJab.controller"); State(bareController, "PunchJab", punch, punch.length / v.punchDuration);
            v.libraryBare.runtimeAnimatorController = bareController;
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/05_Characters/WarriorPrototype/Guerrero_PielYRopa.mat");
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.sharedMaterial = material;
            model.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static AnimatorController Controller(string path)
    { return AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path); }
    static void State(AnimatorController c, string name, AnimationClip clip, float speed)
    {
        var machine = c.layers[0].stateMachine; var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
        state.motion = clip; state.speed = speed; if (name == "Idle") machine.defaultState = state; EditorUtility.SetDirty(state);
    }
    static AnimationClip Clip(string path, string suffix)
    { return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__") && c.name.Split('|').Last() == suffix); }
    static Bounds Bounds(GameObject go)
    { var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b; }
}
