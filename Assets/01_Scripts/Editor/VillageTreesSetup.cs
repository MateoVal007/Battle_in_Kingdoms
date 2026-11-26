using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class VillageTreesSetup
{
    const string ArtFolder = "Assets/07_Models/Trees/";
    const string ModelPath = ArtFolder + "TreeModel.obj";
    const string PrefabFolder = "Assets/02_Prefabs/Resources/LevelModels/";
    const string GreenPrefabPath = PrefabFolder + "TreeGreen.prefab";
    const string AutumnPrefabPath = PrefabFolder + "TreeAutumn.prefab";

    static readonly string[] TargetScenes =
    {
        "Assets/00_Scenes/Pueblo_01_Blockout.unity",
        "Assets/00_Scenes/Pueblo_Online.unity",
    };

    static readonly HashSet<string> TrunkNames = new HashSet<string>
    {
        "Tronco",
        "Tronco bosque",
        "Tronco camino",
    };

    static readonly HashSet<string> CrownNames = new HashSet<string>
    {
        "Copa",
        "Follaje bosque",
        "Copa camino real",
    };

    [MenuItem("Battle in Kingdoms/Actualizar árboles del Pueblo")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Sal de Play Mode antes de actualizar los árboles del Pueblo.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();
        EnsureFolder(PrefabFolder.TrimEnd('/'));
        ConfigureTextures();

        var barkMat = CreateOrUpdateBarkMaterial(ArtFolder + "Tree_Bark.mat", ArtFolder + "Tree_Bark.jpg");
        var greenCoreMat = CreateOrUpdateCanopyCoreMaterial(ArtFolder + "Tree_CanopyCore_Green.mat", ArtFolder + "Tree_Leaves_Green.png", new Color(0.84f, 0.90f, 0.82f, 1f));
        var greenLeavesMat = CreateOrUpdateLeafCardsMaterial(ArtFolder + "Tree_LeafCards_Green.mat", ArtFolder + "Tree_Leaves_Green.png");
        var autumnCoreMat = CreateOrUpdateCanopyCoreMaterial(ArtFolder + "Tree_CanopyCore_Autumn.mat", ArtFolder + "Tree_Leaves_Autumn.png", new Color(0.90f, 0.82f, 0.72f, 1f));
        var autumnLeavesMat = CreateOrUpdateLeafCardsMaterial(ArtFolder + "Tree_LeafCards_Autumn.mat", ArtFolder + "Tree_Leaves_Autumn.png");

        ConfigureModel(barkMat, greenCoreMat, greenLeavesMat);

        var greenPrefab = BuildTreePrefab(GreenPrefabPath, "TreeGreen", barkMat, greenCoreMat, greenLeavesMat);
        var autumnPrefab = BuildTreePrefab(AutumnPrefabPath, "TreeAutumn", barkMat, autumnCoreMat, autumnLeavesMat);

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

                int replaced = ApplyToScene(scenePath, greenPrefab, autumnPrefab);
                Debug.Log($"TREES_SCENE_UPDATED {scenePath} replaced={replaced}");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScene) && File.Exists(activeScene))
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("TREES_BUILD_PASS");
    }

    public static void BuildAndCapture()
    {
        Build();

        string outDir = GetArg("-treeCaptureDir");
        if (string.IsNullOrEmpty(outDir)) return;
        Directory.CreateDirectory(outDir);

        EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity", OpenSceneMode.Single);
        var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
        if (!cam)
        {
            Debug.LogError("No se encontró cámara en Pueblo_01_Blockout para captura.");
            return;
        }

        CaptureView(cam, new Vector3(11f, 5.8f, -20f), Quaternion.Euler(6f, 0f, 0f), Path.Combine(outDir, "sector01_llegada.png"));
        CaptureView(cam, new Vector3(153f, 5.8f, -18f), Quaternion.Euler(6f, 0f, 0f), Path.Combine(outDir, "sector07_bosque.png"));
        CaptureView(cam, new Vector3(212f, 6.2f, -20f), Quaternion.Euler(6f, 0f, 0f), Path.Combine(outDir, "sector09_camino.png"));
        Debug.Log("TREES_CAPTURE_PASS " + outDir);
    }

    static void CaptureView(Camera cam, Vector3 pos, Quaternion rot, string outPath)
    {
        var origPos = cam.transform.position;
        var origRot = cam.transform.rotation;
        var origTarget = cam.targetTexture;
        var rt = new RenderTexture(1280, 720, 24);
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            cam.targetTexture = origTarget;
            cam.transform.SetPositionAndRotation(origPos, origRot);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void ConfigureTextures()
    {
        foreach (string leafTex in new[] { ArtFolder + "Tree_Leaves_Green.png", ArtFolder + "Tree_Leaves_Autumn.png" })
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(leafTex);
            if (!ti) throw new Exception("No se encontró textura: " + leafTex);
            ti.textureType = TextureImporterType.Default;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 512;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        var barkTi = (TextureImporter)AssetImporter.GetAtPath(ArtFolder + "Tree_Bark.jpg");
        if (!barkTi) throw new Exception("No se encontró textura de corteza.");
        barkTi.textureType = TextureImporterType.Default;
        barkTi.mipmapEnabled = true;
        barkTi.maxTextureSize = 512;
        barkTi.textureCompression = TextureImporterCompression.CompressedHQ;
        barkTi.SaveAndReimport();
    }

    static Material LoadOrCreateLit(string path)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader == null || mat.shader.name != "Universal Render Pipeline/Lit")
        {
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        return mat;
    }

    static Material CreateOrUpdateBarkMaterial(string path, string texPath)
    {
        var mat = LoadOrCreateLit(path);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.08f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", (float)CullMode.Back);
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = (int)RenderQueue.Geometry;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateOrUpdateCanopyCoreMaterial(string path, string texPath, Color tint)
    {
        var mat = LoadOrCreateLit(path);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.10f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", (float)CullMode.Back);
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = (int)RenderQueue.Geometry;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateOrUpdateLeafCardsMaterial(string path, string texPath)
    {
        var mat = LoadOrCreateLit(path);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.12f);
        mat.SetFloat("_Surface", 0f);
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.45f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", (float)CullMode.Off);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.renderQueue = (int)RenderQueue.AlphaTest;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigureModel(Material barkMat, Material coreMat, Material leavesMat)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        if (!importer) throw new Exception("No se encontró el modelo: " + ModelPath);
        importer.importCameras = false;
        importer.importLights = false;
        importer.importBlendShapes = false;
        importer.importAnimation = false;
        importer.addCollider = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Tree_Bark"), barkMat);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Tree_CanopyCore"), coreMat);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Tree_LeafCards"), leavesMat);
        importer.SaveAndReimport();
    }

    static GameObject BuildTreePrefab(string prefabPath, string rootName, Material barkMat, Material coreMat, Material leavesMat)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!source) throw new Exception("No se pudo cargar GameObject de " + ModelPath);

        var root = new GameObject(rootName);
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            instance.name = "Mesh";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (var col in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);

            foreach (var r in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string mname = mats[i] ? mats[i].name : r.gameObject.name;
                    if (mname.Contains("Bark") || r.gameObject.name.Contains("Bark"))
                        mats[i] = barkMat;
                    else if (mname.Contains("CanopyCore") || r.gameObject.name.Contains("CanopyCore"))
                        mats[i] = coreMat;
                    else
                        mats[i] = leavesMat;
                }
                r.sharedMaterials = mats;
            }

            var bounds = ComputeBounds(root);
            if (bounds.size.y > 0.01f)
            {
                float norm = 8.0f / bounds.size.y;
                instance.transform.localScale *= norm;
                bounds = ComputeBounds(root);
                instance.transform.localPosition -= new Vector3(0f, bounds.min.y, 0f);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"TREE_PREFAB_SAVED {prefabPath} height={ComputeBounds(root).size.y:F2}");
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static int ApplyToScene(string scenePath, GameObject greenPrefab, GameObject autumnPrefab)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var trunks = allTransforms.Where(t => TrunkNames.Contains(t.name)).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
        var crowns = allTransforms.Where(t => CrownNames.Contains(t.name)).ToList();

        if (trunks.Count == 0)
        {
            Debug.Log($"No quedan troncos primitivos en {scenePath}; verificando árboles ya convertidos.");
            return 0;
        }

        var usedCrowns = new HashSet<Transform>();
        int replaced = 0;

        for (int i = 0; i < trunks.Count; i++)
        {
            var trunk = trunks[i];
            var crown = FindMatchingCrown(trunk, crowns, usedCrowns);
            if (crown != null) usedCrowns.Add(crown);

            var trunkBounds = GetRendererBounds(trunk.gameObject);
            float baseY = trunkBounds.HasValue ? trunkBounds.Value.min.y : 0f;
            float topY = crown != null && GetRendererBounds(crown.gameObject).HasValue
                ? GetRendererBounds(crown.gameObject).Value.max.y
                : (trunkBounds.HasValue ? trunkBounds.Value.max.y + 4.0f : 8.5f);

            float targetHeight = Mathf.Clamp(topY - baseY, 6.5f, 13.5f);
            float scale = targetHeight / 8.0f;

            // Keep trees mostly lush green with warm autumn accents (~1 in 4) using both variants from trees.glb.
            bool useAutumn = (i % 4 == 2);
            var prefab = useAutumn ? autumnPrefab : greenPrefab;
            var treeObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, trunk.parent);
            treeObj.name = useAutumn ? $"Árbol otoñal · {i + 1:00}" : $"Árbol · {i + 1:00}";
            treeObj.transform.position = new Vector3(trunk.position.x, baseY - 0.08f, trunk.position.z);
            treeObj.transform.rotation = Quaternion.Euler(0f, (i * 57f + 18f) % 360f, 0f);
            treeObj.transform.localScale = Vector3.one * scale;

            UnityEngine.Object.DestroyImmediate(trunk.gameObject);
            if (crown != null) UnityEngine.Object.DestroyImmediate(crown.gameObject);
            replaced++;
        }

        // Remove any orphan crowns left behind
        foreach (var orphan in crowns.Where(c => c != null && !usedCrowns.Contains(c)))
            UnityEngine.Object.DestroyImmediate(orphan.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return replaced;
    }

    static Transform FindMatchingCrown(Transform trunk, List<Transform> crowns, HashSet<Transform> used)
    {
        Transform best = null;
        float bestDist = 1.5f;
        foreach (var c in crowns)
        {
            if (c == null || used.Contains(c)) continue;
            float dx = c.position.x - trunk.position.x;
            float dz = c.position.z - trunk.position.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    static Bounds? GetRendererBounds(GameObject go)
    {
        var r = go.GetComponent<Renderer>();
        return r ? r.bounds : (Bounds?)null;
    }

    static string GetArg(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key) return args[i + 1];
        return null;
    }

    static Bounds ComputeBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
