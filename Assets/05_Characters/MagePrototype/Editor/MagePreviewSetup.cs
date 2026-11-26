using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valeriano.CharacterPreview;
using static WarriorPreviewSetup;

public static class MagePreviewSetup
{
    const string Folder = "Assets/05_Characters/MagePrototype/";
    const string ModelPath = Folder + "Mago.fbx";
    const string ControllerPath = Folder + "MagoPrueba.controller";

    [MenuItem("Battle in Kingdoms/Preparar prueba del mago")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Guarda la escena activa y sal de Play Mode antes de preparar la prueba.");

        var model = ImportHumanoidAtPath(ModelPath, name => name.Contains("Idle_Mago"), true);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
        if (!avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("El avatar del mago no es válido.");

        var clips = CharacterMotionSetup.MageMotions();
        clips.Add("Idle", ReadHumanoidClip(ModelPath, "Idle_Mago"));
        clips.Add("Cast", ReadHumanoidClip(ModelPath, "Lanzamiento_Baston"));
        var controller = BuildController(ControllerPath, clips);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = new GameObject("Mago de prueba");
        var motor = player.AddComponent<CharacterController>();
        motor.height = 1.8f;
        motor.center = Vector3.up * .9f;
        motor.radius = .34f;

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        visual.transform.SetParent(player.transform, false);
        visual.transform.localRotation = Quaternion.Euler(0, 137, 0);
        var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        SetMaterials(visual, Folder, "Mago_Tunica", "Baston");

        var camera = CreateStage(Folder);
        camera.orthographicSize = 1.82f;
        camera.transform.LookAt(new Vector3(0, 1.45f, 0));
        var preview = player.AddComponent<MagePreview>();
        preview.animator = animator;
        preview.motor = motor;
        preview.staff = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Baston");

        EditorSceneManager.SaveScene(scene, Folder + "Mago_Prueba.unity");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        Debug.Log($"MAGO_PRUEBA avatar={avatar.isValid}/{avatar.isHuman}, clips={clips.Count}");
    }
}
