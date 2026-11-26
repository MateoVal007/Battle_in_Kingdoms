using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VillageGrassSetup
{
    const string ArtFolder = "Assets/07_Models/Grass1/";
    const string ModelPath = ArtFolder + "Grass1.fbx";
    const string PrefabFolder = "Assets/02_Prefabs/Resources/LevelModels/";
    const string PrefabPath = PrefabFolder + "Grass1.prefab";
    const string RootName = "Pasto · Grass1";

    static readonly string[] TargetScenes =
    {
        "Assets/00_Scenes/Pueblo_01_Blockout.unity",
        "Assets/00_Scenes/Pueblo_Online.unity",
    };

    [MenuItem("Battle in Kingdoms/Poblar pasto Grass1 (Pueblo)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Sal de Play Mode antes de poblar el pasto Grass1.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();
        EnsureFolder(PrefabFolder.TrimEnd('/'));

        GameplayPresentationSetup.SurfacesFor(ArtFolder, "Grass1");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(ArtFolder + "Grass1_Mat.mat");
        if (mat)
        {
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }

        var prefab = BuildPrefab(mat, out Vector3 prefabSize);
        AssetDatabase.SaveAssets();

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

                int placed = ApplyToScene(scenePath, prefab, prefabSize);
                Debug.Log($"GRASS1_SCENE_UPDATED {scenePath} placed={placed} (aligned grid on bare green meadow gaps only)");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScene) && File.Exists(activeScene))
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("GRASS1_BUILD_PASS");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static GameObject BuildPrefab(Material grassMat, out Vector3 prefabSize)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!source) throw new Exception("No se encontró " + ModelPath);

        var root = new GameObject("Grass1");
        try
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            inst.name = "Mesh";
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localScale = Vector3.one;
            inst.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);

            if (grassMat)
            {
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                    r.sharedMaterial = grassMat;
            }

            var b = ComputeBounds(root);
            if (b.size.y > b.size.x * 0.5f || b.size.y > b.size.z * 0.5f)
            {
                inst.transform.localRotation = Quaternion.identity;
                b = ComputeBounds(root);
            }

            inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            b = ComputeBounds(root);
            prefabSize = b.size;
            Debug.Log($"GRASS1_PREFAB_FLAT size=({b.size.x:F3}, {b.size.y:F3}, {b.size.z:F3})");

            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static int ApplyToScene(string scenePath, GameObject grassPrefab, Vector3 prefabSize)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var oldRoot = GameObject.Find(RootName);
        if (oldRoot) UnityEngine.Object.DestroyImmediate(oldRoot);

        var root = new GameObject(RootName).transform;

        var allRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // 1. Find all bare lime-green meadow rectangles ("Pradera de fondo")
        var meadows = allRenderers
            .Where(r => r.enabled && r.gameObject.name == "Pradera de fondo")
            .OrderBy(r => r.bounds.min.x)
            .ToList();

        // 2. Find all existing cobblestone house-ground pads (HouseGrounds) so grass NEVER overlaps onto cobblestone!
        var cobblestonePads = new List<Bounds>();
        foreach (var r in allRenderers)
        {
            if (!r.enabled) continue;
            var mf = r.GetComponent<MeshFilter>();
            string meshPath = mf && mf.sharedMesh ? AssetDatabase.GetAssetPath(mf.sharedMesh) : "";
            bool isHouseGround = meshPath.Contains("HouseGrounds");
            if (!isHouseGround)
            {
                // Fallback: any Ground-material surface covering z > 2.2 (house yards)
                bool hasGroundMat = r.sharedMaterials.Any(m => m && m.name == "Ground");
                if (hasGroundMat && r.bounds.max.z > 2.4f && r.bounds.size.z > 2.5f)
                    isHouseGround = true;
            }
            if (isHouseGround)
                cobblestonePads.Add(r.bounds);
        }

        Debug.Log($"GRASS1_ANALYSIS {scenePath}: meadows={meadows.Count}, cobblestonePads={cobblestonePads.Count}");

        float baseW = Mathf.Max(0.1f, prefabSize.x);
        float baseD = Mathf.Max(0.1f, prefabSize.z);
        int placed = 0;

        const int rowsPerMeadow = 4;     // 10m depth / 4 = 2.5m per row cleanly aligned
        const float targetCellW = 2.55f; // balanced square-ish tiles
        const float edgeSeal = 1.03f;    // tiny 3% seal so zero lime-green cracks peek through

        foreach (var meadow in meadows)
        {
            Bounds mb = meadow.bounds;
            float floorY = mb.max.y + 0.008f;
            float rowDepth = mb.size.z / rowsPerMeadow;

            for (int row = 0; row < rowsPerMeadow; row++)
            {
                float z0 = mb.min.z + row * rowDepth;
                float z1 = z0 + rowDepth;

                // Start with the full meadow X span and subtract every cobblestone yard overlapping this Z strip
                var uncovered = new List<(float minX, float maxX)> { (mb.min.x, mb.max.x) };

                foreach (var pad in cobblestonePads)
                {
                    if (pad.max.z <= z0 + 0.15f || pad.min.z >= z1 - 0.15f)
                        continue;

                    uncovered = SubtractInterval(uncovered, pad.min.x - 0.05f, pad.max.x + 0.05f);
                    if (uncovered.Count == 0) break;
                }

                // Tile each uncovered lime-green rectangle edge-to-edge with right-angle aligned Grass1 tiles
                foreach (var (segMinX, segMaxX) in uncovered)
                {
                    float segW = segMaxX - segMinX;
                    if (segW < 0.35f) continue;

                    int cols = Mathf.Max(1, Mathf.RoundToInt(segW / targetCellW));
                    float cellW = segW / cols;
                    float cellD = rowDepth;

                    for (int col = 0; col < cols; col++)
                    {
                        float cx = segMinX + (col + 0.5f) * cellW;
                        float cz = (z0 + z1) * 0.5f;

                        // Right-angle 0/90/180/270 rotations ONLY -> zero diagonal overlaps, subtle grass blade variety
                        int rotStep = (placed * 3 + row * 2 + col) & 3;
                        float yaw = rotStep * 90f;
                        bool swapped = (rotStep % 2) == 1;

                        float sx = ((swapped ? cellD : cellW) / baseW) * edgeSeal;
                        float sz = ((swapped ? cellW : cellD) / baseD) * edgeSeal;
                        float sy = 1.45f; // crisp ~22 cm tall 3D grass blades

                        var tile = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, root);
                        tile.name = $"Pasto · {placed + 1:000}";
                        tile.transform.position = new Vector3(cx, floorY, cz);
                        tile.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                        tile.transform.localScale = new Vector3(sx, sy, sz);
                        placed++;
                    }
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return placed;
    }

    static List<(float minX, float maxX)> SubtractInterval(List<(float minX, float maxX)> spans, float cutMin, float cutMax)
    {
        var result = new List<(float minX, float maxX)>();
        foreach (var (a, b) in spans)
        {
            if (cutMax <= a || cutMin >= b)
            {
                result.Add((a, b));
            }
            else
            {
                if (cutMin > a + 0.05f) result.Add((a, cutMin));
                if (cutMax < b - 0.05f) result.Add((cutMax, b));
            }
        }
        return result;
    }

    static Bounds ComputeBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
