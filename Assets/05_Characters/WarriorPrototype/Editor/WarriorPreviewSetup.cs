using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valeriano.CharacterPreview;

public static class WarriorPreviewSetup
{
    const string Root = "Assets/05_Characters/WarriorPrototype/";
    const string ScenePath = Root + "Guerrero_Prueba.unity";
    const string ControllerPath = Root + "GuerreroPrueba.controller";

    const string EquippedFolder = "Assets/05_Characters/WarriorEquipped/";
    const string EquippedModel = EquippedFolder + "GuerreroArmaduraJugable.fbx";

    [MenuItem("Battle in Kingdoms/Preparar prueba del guerrero")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Guarda la escena activa y sal de Play Mode antes de preparar la prueba.");

        var controller = BuildController(ControllerPath, CharacterMotionSetup.WarriorMotions());
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "GuerreroSinEquipoJugable.fbx");
        var equippedModel = AssetDatabase.LoadAssetAtPath<GameObject>(EquippedModel);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = new GameObject("Guerrero de prueba");
        var motor = player.AddComponent<CharacterController>();
        motor.height = 1.8f;
        motor.center = Vector3.up * .9f;
        motor.radius = .32f;
        motor.stepOffset = .24f;
        var pivot = new GameObject("Visual");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localRotation = Quaternion.Euler(0, WarriorPreview.FacingYaw, 0);
        var animator = InstantiateVisual(model, scene, pivot.transform, controller, false);
        var armoredAnimator = InstantiateVisual(equippedModel, scene, pivot.transform, controller, false);
        var visual = animator.gameObject;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            if (!(renderer is SkinnedMeshRenderer)) renderer.gameObject.SetActive(false);
        SetMaterials(visual, Root, "Guerrero_PielYRopa");
        SetMaterials(armoredAnimator.gameObject, EquippedFolder, "Guerrero_Original");
        armoredAnimator.gameObject.SetActive(false);

        var camera = CreateStage(Root);

        var preview = player.AddComponent<WarriorPreview>();
        preview.animator = animator;
        preview.armoredAnimator = armoredAnimator;
        preview.visual = pivot.transform;
        preview.motor = motor;
        preview.view = camera;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        Debug.Log("GUERRERO_MODELOS modelo base y rig equipado vinculado");
    }

    static Animator InstantiateVisual(GameObject model, Scene scene, Transform parent, RuntimeAnimatorController controller, bool humanoid = true)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        instance.transform.SetParent(parent, false);
        var animator = instance.GetComponent<Animator>();
        if (!animator) animator = instance.AddComponent<Animator>();
        if (humanoid)
        {
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<Avatar>().Single();
            if (!animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Avatar humanoide inválido: " + model.name);
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return animator;
    }

    internal static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var child in machine.states) machine.RemoveState(child.state);
        foreach (var clip in clips)
        {
            var state = machine.AddState(clip.Key);
            state.motion = clip.Value;
            if (clip.Key == "Idle") machine.defaultState = state;
        }
        return controller;
    }

    internal static Camera CreateStage(string folder)
    {
        var camera = new GameObject("Cámara de prueba").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 1.68f;
        camera.transform.position = new Vector3(.35f, 1.45f, -5.6f);
        camera.transform.LookAt(new Vector3(0, 1.2f, 0));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.12f, .17f, .24f);
        new GameObject("Audio").AddComponent<AudioListener>().transform.SetParent(camera.transform, false);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Pista de animación";
        floor.transform.position = new Vector3(0, -.2f, 0);
        floor.transform.localScale = new Vector3(44, .4f, 5);
        floor.GetComponent<Renderer>().sharedMaterial = ColorMaterial("Pista", new Color(.23f, .31f, .34f), folder);
        AddLight("Luz principal", new Vector3(36, -38, 0), 1.7f);
        AddLight("Relleno", new Vector3(28, 150, 0), .58f);
        RenderSettings.ambientLight = new Color(.56f, .59f, .64f);
        return camera;
    }

    internal static GameObject ImportHumanoidAtPath(string path, Func<string, bool> loops, bool explicitMapping = false) =>
        ImportAnimatedAtPath(path, loops, ModelImporterAnimationType.Human, explicitMapping);

    internal static GameObject ImportGenericAtPath(string path, Func<string, bool> loops) =>
        ImportAnimatedAtPath(path, loops, ModelImporterAnimationType.Generic, false);

    static GameObject ImportAnimatedAtPath(string path, Func<string, bool> loops,
        ModelImporterAnimationType animationType, bool explicitMapping)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (!importer) throw new InvalidOperationException("FBX ausente: " + path);
        importer.animationType = animationType;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
        importer.importCameras = false;
        importer.importLights = false;
        var defaults = importer.defaultClipAnimations;
        foreach (var clip in defaults)
        {
            clip.loopTime = loops(clip.name);
            clip.loopPose = clip.loopTime;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionXZ = true;
            if (animationType == ModelImporterAnimationType.Generic) clip.keepOriginalPositionY = true;
        }
        if (defaults.Length > 0) importer.clipAnimations = defaults;
        if (explicitMapping)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var transforms = source.GetComponentsInChildren<Transform>(true);
            var names = new HashSet<string>(transforms.Select(item => item.name));
            var description = importer.humanDescription;
            description.human = HumanTrait.BoneName
                .Select(humanName => new HumanBone
                {
                    humanName = humanName,
                    boneName = humanName.Replace(" ", ""),
                    limit = new HumanLimit { useDefaultValues = true }
                })
                .Where(bone => names.Contains(bone.boneName))
                .ToArray();
            description.skeleton = transforms.Select(item => new SkeletonBone
            {
                name = item.name,
                position = item.localPosition,
                rotation = item.localRotation,
                scale = item.localScale
            }).ToArray();
            importer.humanDescription = description;
        }
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    internal static AnimationClip ReadHumanoidClip(string path, string namePart = null)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(asset => !asset.name.StartsWith("__preview__")
                && (namePart == null || asset.name.Contains(namePart)));
        if (!clip || !clip.humanMotion)
            throw new InvalidOperationException("No se encontró el clip humanoide: " + path + " " + namePart);
        return clip;
    }

    internal static void SetMaterials(GameObject visual, string folder, string skinName, string texturedProp = null)
    {
        var skin = ColorMaterial(skinName, Color.white, folder);
        skin.SetTexture("_BaseMap", ImportSurfaceTexture(folder + "Albedo.png", TextureImporterType.Default));
        skin.SetTexture("_BumpMap", ImportSurfaceTexture(folder + "Normal.png", TextureImporterType.NormalMap));
        skin.SetFloat("_BumpScale", .7f);
        skin.EnableKeyword("_NORMALMAP");
        skin.SetFloat("_Smoothness", .28f);
        var gearMaterials = new Dictionary<string, Material>();
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            // The repaired arm uses the unarmored sculpt's UVs. Keep its own
            // surface and the shield's wooden back when rebuilding previews.
            if (folder == EquippedFolder && renderer.name == "Brazo_Izquierdo_Restaurado")
            {
                var armSurface = AssetDatabase.LoadAssetAtPath<Material>(folder + "Guerrero_BrazoRestaurado.mat");
                if (armSurface) { renderer.sharedMaterial = armSurface; continue; }
            }
            if (folder == EquippedFolder && renderer.name == "Escudo_Original_Rigido")
            {
                var shieldBack = AssetDatabase.LoadAssetAtPath<Material>(folder + "Escudo_InteriorReparado.mat");
                if (shieldBack && renderer.sharedMaterials.Length == 2)
                {
                    renderer.sharedMaterials = new[] { skin, shieldBack };
                    continue;
                }
            }
            if (renderer is SkinnedMeshRenderer || renderer.name == texturedProp)
            {
                renderer.sharedMaterial = skin;
                continue;
            }
            renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
            {
                string name = source ? source.name : "Metal";
                if (gearMaterials.TryGetValue(name, out var material)) return material;
                Color color = source && source.HasProperty("_Color") ? source.GetColor("_Color")
                    : source && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : new Color(.49f, .54f, .63f);
                string safeName = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
                material = ColorMaterial("Pieza_" + safeName, color, folder);
                switch (name)
                {
                    case "Acero_plata": StyleGear(material, new Color32(155, 171, 193, 255), .42f, .40f); break;
                    case "Acero_oscuro": StyleGear(material, new Color32(61, 73, 89, 255), .48f, .35f); break;
                    case "Laton_bordes": StyleGear(material, new Color32(168, 115, 55, 255), .52f, .38f); break;
                    case "Cuero_marron": StyleGear(material, new Color32(83, 49, 33, 255), 0f, .23f); break;
                    case "Escudo_azul_marino": StyleGear(material, new Color32(31, 65, 124, 255), .17f, .36f); break;
                }
                gearMaterials.Add(name, material);
                return material;
            }).ToArray();
        }
    }

    static Texture2D ImportSurfaceTexture(string path, TextureImporterType type)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != type || importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureType = type;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void StyleGear(Material material, Color color, float metallic, float smoothness)
    {
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
    }

    static Material ColorMaterial(string name, Color color, string folder)
    {
        var path = folder + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void AddLight(string name, Vector3 angle, float strength)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = strength;
        light.transform.rotation = Quaternion.Euler(angle);
    }
}

