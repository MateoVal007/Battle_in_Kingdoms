using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VillagePlatformGapFix
{
    static readonly string[] TargetScenes =
    {
        "Assets/00_Scenes/Pueblo_01_Blockout.unity",
        "Assets/00_Scenes/Pueblo_Online.unity",
    };

    // Target geometry opening a 1.60 m clear vertical passage (x=66.40..68.00)
    // between the upper step (Warrior platform) and the lower stacked step (Mage jump).
    const float UpperCenterX = 65.45f;
    const float UpperWidth = 1.90f;
    const float UpperCliffScaleX = 0.415f;
    const float UpperPostX = 64.68f;

    const float LowerCenterX = 68.75f;
    const float LowerWidth = 1.50f;
    const float LowerCliffScaleX = 0.325f;

    [MenuItem("Battle in Kingdoms/Corregir abertura de plataformas (Tejados)")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Sal de Play Mode antes de corregir las plataformas.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

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

                int adjusted = ApplyToScene(scenePath);
                Debug.Log($"PLATFORM_GAP_FIX {scenePath} adjusted={adjusted} opening=1.60m");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScene) && File.Exists(activeScene))
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("PLATFORM_GAP_FIX_PASS");
    }

    static int ApplyToScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        foreach (var t in transforms)
        {
            Vector3 p = t.position;
            if (Mathf.Abs(p.z) > 2.0f) continue;

            // 1. Upper platform (previously x=66.00, y=2.60, width=2.40)
            if (p.x >= 65.0f && p.x <= 66.5f)
            {
                if (t.name == "Plataforma de madera" && Mathf.Abs(p.y - 2.60f) < 0.25f)
                {
                    SetPosScaleX(t, UpperCenterX, UpperWidth);
                    count++;
                }
                else if (t.name == "Borde claro" && Mathf.Abs(p.y - 2.83f) < 0.25f)
                {
                    SetPosScaleX(t, UpperCenterX, UpperWidth);
                    count++;
                }
                else if (t.name == "ClifGg2" && Mathf.Abs(p.y - 2.34f) < 0.35f)
                {
                    SetPosScaleX(t, UpperCenterX, UpperCliffScaleX);
                    count++;
                }
            }

            // Left support post under upper platform (previously x=64.98, y=0.96, z=1.05)
            if (t.name == "Puntal de pasarela" && p.x >= 64.4f && p.x <= 65.3f && Mathf.Abs(p.y - 0.96f) < 0.35f)
            {
                t.position = new Vector3(UpperPostX, p.y, p.z);
                EditorUtility.SetDirty(t);
                count++;
            }

            // 2. Lower stacked platform (previously x=68.50, y=1.20 / 0.45, width=2.00)
            if (p.x >= 68.1f && p.x <= 69.2f)
            {
                if (t.name == "Plataforma de madera" && Mathf.Abs(p.y - 1.20f) < 0.25f)
                {
                    SetPosScaleX(t, LowerCenterX, LowerWidth);
                    count++;
                }
                else if (t.name.StartsWith("Terrapl") && Mathf.Abs(p.y - 0.45f) < 0.30f)
                {
                    SetPosScaleX(t, LowerCenterX, LowerWidth);
                    count++;
                }
                else if (t.name == "ClifGg2" && (Mathf.Abs(p.y - 0.94f) < 0.35f || Mathf.Abs(p.y + 0.11f) < 0.35f))
                {
                    SetPosScaleX(t, LowerCenterX, LowerCliffScaleX);
                    count++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return count;
    }

    static void SetPosScaleX(Transform t, float newX, float newScaleX)
    {
        var p = t.position;
        t.position = new Vector3(newX, p.y, p.z);
        var s = t.localScale;
        t.localScale = new Vector3(newScaleX, s.y, s.z);
        EditorUtility.SetDirty(t);
    }
}
