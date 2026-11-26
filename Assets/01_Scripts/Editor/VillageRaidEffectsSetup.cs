using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class VillageRaidEffectsSetup
{
    const string MatFolder = "Assets/08_VFX/Materials/";
    const string PrefabFolder = "Assets/02_Prefabs/Resources/VillageVFX/";
    const string EffectsRootName = "Efectos ataque · Pueblo";

    const string MainOriginalMatPath = "Assets/07_Models/MedievalModular/Materiales/MAIN.mat";
    const string RoofOriginalMatPath = "Assets/07_Models/MedievalModular/Materiales/ROOFM.mat";
    const string MainTexturePath = "Assets/07_Models/MedievalModular/Texturas/Texture_1.png";
    const string RoofTexturePath = "Assets/07_Models/MedievalModular/Texturas/Texture_2.png";
    const string SparkMatPath = "Assets/Resources/VFX/FireSpark.mat";

    static readonly string[] TargetScenes =
    {
        "Assets/00_Scenes/Pueblo_01_Blockout.unity",
        "Assets/00_Scenes/Pueblo_Online.unity",
    };

    struct RaidedHouseSpec
    {
        public string name;
        public float sideBiasX;     // -1 left .. +1 right of house bounds
        public float burnHeightPct; // 0 bottom .. 1 top of bounds
        public float burnRadius;
        public float burnIntensity;
        public float emberStrength;
        public float roofScale;
        public bool addSmallWallFire;
    }

    static readonly RaidedHouseSpec[] RaidedHouses =
    {
        new RaidedHouseSpec
        {
            name = "Casa · BH2",
            sideBiasX = 0.28f,
            burnHeightPct = 0.48f,
            burnRadius = 3.5f,
            burnIntensity = 0.78f,
            emberStrength = 0.55f,
            roofScale = 0.95f,
            addSmallWallFire = true,
        },
        new RaidedHouseSpec
        {
            name = "Casa · BH7",
            sideBiasX = -0.22f,
            burnHeightPct = 0.52f,
            burnRadius = 4.2f,
            burnIntensity = 0.90f,
            emberStrength = 0.72f,
            roofScale = 1.15f,
            addSmallWallFire = true,
        },
        new RaidedHouseSpec
        {
            name = "Casa · RH4",
            sideBiasX = -0.25f,
            burnHeightPct = 0.46f,
            burnRadius = 3.6f,
            burnIntensity = 0.82f,
            emberStrength = 0.60f,
            roofScale = 1.0f,
            addSmallWallFire = true,
        },
    };

    static readonly Vector3[] GrassPatchLocations =
    {
        new Vector3(11.5f, 0f, 4.2f),  // 01 Llegada (cerca de Casa BH2)
        new Vector3(34.0f, 0f, 4.0f),  // 02 Huertos
        new Vector3(54.5f, 0f, 4.2f),  // 03 Tejados (cerca de Casa BH7)
        new Vector3(101.5f, 0f, 4.4f), // 05 Plaza (cerca de Casa RH4)
        new Vector3(152.5f, 0f, 3.8f), // 07 Bosque
    };

    [MenuItem("Battle in Kingdoms/Aplicar daño y fuego de ataque (Pueblo)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Sal de Play Mode antes de aplicar los efectos de ataque.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();
        EnsureFolder(MatFolder.TrimEnd('/'));
        EnsureFolder(PrefabFolder.TrimEnd('/'));

        var mainScorched = CreateOrUpdateScorchedMat(MatFolder + "House_MAIN_Scorched.mat", MainTexturePath);
        var roofScorched = CreateOrUpdateScorchedMat(MatFolder + "House_ROOFM_Scorched.mat", RoofTexturePath);

        var fireRoofMat = CreateOrUpdateFireMat(MatFolder + "Fire_HouseRoof.mat", 2.3f, 0.78f, 0.48f, 1.3f);
        var fireSmallMat = CreateOrUpdateFireMat(MatFolder + "Fire_HouseSmall.mat", 2.7f, 0.62f, 0.44f, 4.7f);
        var fireGrassMat = CreateOrUpdateFireMat(MatFolder + "Fire_GrassLow.mat", 2.5f, 0.46f, 0.38f, 9.1f);
        var scorchPatchMat = CreateOrUpdateScorchPatchMat(MatFolder + "Grass_ScorchPatch.mat");
        var sparkMat = AssetDatabase.LoadAssetAtPath<Material>(SparkMatPath);

        var roofFirePrefab = BuildFirePrefab(PrefabFolder + "Fire_HouseRoof.prefab", "Fire_HouseRoof", fireRoofMat, sparkMat, 2.0f, 3.2f, 6f);
        var smallFirePrefab = BuildFirePrefab(PrefabFolder + "Fire_HouseSmall.prefab", "Fire_HouseSmall", fireSmallMat, sparkMat, 1.15f, 1.95f, 3f);
        var grassPatchPrefab = BuildGrassPatchPrefab(PrefabFolder + "Fire_GrassPatch.prefab", scorchPatchMat, fireGrassMat, sparkMat);

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

                ApplyToScene(scenePath, mainScorched, roofScorched, roofFirePrefab, smallFirePrefab, grassPatchPrefab);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScene) && File.Exists(activeScene))
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("RAID_EFFECTS_BUILD_PASS");
    }

    [MenuItem("Battle in Kingdoms/Quitar daño y fuego de ataque (Pueblo)")]
    public static void Remove()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Sal de Play Mode antes de quitar los efectos de ataque.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var mainOrig = AssetDatabase.LoadAssetAtPath<Material>(MainOriginalMatPath);
        var roofOrig = AssetDatabase.LoadAssetAtPath<Material>(RoofOriginalMatPath);
        string activeScene = EditorSceneManager.GetActiveScene().path;

        try
        {
            foreach (string scenePath in TargetScenes)
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var oldRoot = GameObject.Find(EffectsRootName);
                if (oldRoot) UnityEngine.Object.DestroyImmediate(oldRoot);

                foreach (var target in UnityEngine.Object.FindObjectsByType<VillageScorchTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    RestoreHouseMaterials(target.gameObject, mainOrig, roofOrig);
                    UnityEngine.Object.DestroyImmediate(target);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("RAID_EFFECTS_REMOVED " + scenePath);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(activeScene) && File.Exists(activeScene))
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Material LoadOrCreateMat(string path, string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (!shader) throw new Exception("No se encontró shader: " + shaderName);

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }
        return mat;
    }

    static Material CreateOrUpdateScorchedMat(string path, string texPath)
    {
        var mat = LoadOrCreateMat(path, "BattleInKingdoms/VillageScorchedLit");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (!tex) throw new Exception("No se encontró textura: " + texPath);
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_CharColor", new Color(0.045f, 0.038f, 0.035f, 1f));
        mat.SetColor("_EmberColor", new Color(2.4f, 0.48f, 0.06f, 1f));
        mat.SetFloat("_BurnRadius", 3.6f);
        mat.SetFloat("_BurnIntensity", 0.82f);
        mat.SetFloat("_SootRise", 1.9f);
        mat.SetFloat("_EmberStrength", 0.6f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateOrUpdateFireMat(string path, float speed, float smokeAmount, float flameHeight, float seed)
    {
        var mat = LoadOrCreateMat(path, "BattleInKingdoms/VillageAmbientFire");
        mat.SetColor("_CoreColor", new Color(3.2f, 2.1f, 0.50f, 1f));
        mat.SetColor("_FlameColor", new Color(2.3f, 0.40f, 0.04f, 1f));
        mat.SetColor("_SmokeColor", new Color(0.10f, 0.095f, 0.095f, 1f));
        mat.SetFloat("_Speed", speed);
        mat.SetFloat("_SmokeAmount", smokeAmount);
        mat.SetFloat("_FlameHeight", flameHeight);
        mat.SetFloat("_Seed", seed);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateOrUpdateScorchPatchMat(string path)
    {
        var mat = LoadOrCreateMat(path, "BattleInKingdoms/VillageScorchPatch");
        mat.SetColor("_CharColor", new Color(0.05f, 0.042f, 0.038f, 1f));
        mat.SetColor("_EdgeColor", new Color(0.17f, 0.14f, 0.085f, 1f));
        mat.SetColor("_EmberColor", new Color(2.3f, 0.50f, 0.06f, 1f));
        mat.SetFloat("_EmberStrength", 0.72f);
        mat.SetFloat("_Seed", 2.5f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject BuildFirePrefab(string path, string rootName, Material fireMat, Material sparkMat, float width, float height, float sparkRate)
    {
        var root = new GameObject(rootName);
        try
        {
            CreateCrossedFireQuads(root.transform, fireMat, width, height, Vector3.zero);
            if (sparkMat) AddEmberParticles(root.transform, sparkMat, width * 0.35f, height * 0.75f, sparkRate);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static GameObject BuildGrassPatchPrefab(string path, Material patchMat, Material fireMat, Material sparkMat)
    {
        var root = new GameObject("Fire_GrassPatch");
        try
        {
            // Ground scorch decal lying flat just above grass surface
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = "MarcaChamuscada";
            UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());
            decal.transform.SetParent(root.transform, false);
            decal.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            decal.transform.localScale = new Vector3(2.8f, 2.1f, 1f);
            var dr = decal.GetComponent<MeshRenderer>();
            dr.sharedMaterial = patchMat;
            dr.shadowCastingMode = ShadowCastingMode.Off;
            dr.receiveShadows = false;

            // Two small localized flames rising from the scorched grass
            CreateCrossedFireQuads(root.transform, fireMat, 0.95f, 1.35f, new Vector3(-0.22f, 0.0f, 0.05f));
            CreateCrossedFireQuads(root.transform, fireMat, 0.65f, 0.95f, new Vector3(0.45f, 0.0f, -0.15f));

            if (sparkMat) AddEmberParticles(root.transform, sparkMat, 0.55f, 1.2f, 3f);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void CreateCrossedFireQuads(Transform parent, Material mat, float width, float height, Vector3 baseOffset)
    {
        foreach (float yaw in new[] { -32f, 32f })
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = yaw < 0 ? "Llama_A" : "Llama_B";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = baseOffset + new Vector3(0f, height * 0.5f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            quad.transform.localScale = new Vector3(width, height, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    static void AddEmberParticles(Transform parent, Material sparkMat, float radius, float riseHeight, float rate)
    {
        var go = new GameObject("Chispas");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.25f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, Mathf.Max(1.1f, riseHeight * 0.65f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.gravityModifier = -0.12f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.45f;
        noise.frequency = 0.9f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = sparkMat;
        pr.shadowCastingMode = ShadowCastingMode.Off;
        pr.receiveShadows = false;
    }

    static void ApplyToScene(
        string scenePath,
        Material mainScorched,
        Material roofScorched,
        GameObject roofFirePrefab,
        GameObject smallFirePrefab,
        GameObject grassPatchPrefab)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var oldRoot = GameObject.Find(EffectsRootName);
        if (oldRoot) UnityEngine.Object.DestroyImmediate(oldRoot);

        var effectsRoot = new GameObject(EffectsRootName).transform;
        int housesUpdated = 0;

        var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var spec in RaidedHouses)
        {
            var house = allTransforms.FirstOrDefault(t => t.name == spec.name);
            if (!house)
            {
                Debug.LogWarning($"Casa no encontrada en {scenePath}: {spec.name}");
                continue;
            }

            SwapHouseMaterialsToScorched(house.gameObject, mainScorched, roofScorched);

            var bounds = ComputeBounds(house.gameObject);
            Vector3 burnWorld = new Vector3(
                bounds.center.x + bounds.extents.x * spec.sideBiasX,
                Mathf.Lerp(bounds.min.y, bounds.max.y, spec.burnHeightPct),
                bounds.min.z + bounds.extents.z * 0.25f
            );

            var scorch = house.GetComponent<VillageScorchTarget>() ?? house.gameObject.AddComponent<VillageScorchTarget>();
            scorch.burnLocalOffset = house.InverseTransformPoint(burnWorld);
            scorch.burnRadius = spec.burnRadius;
            scorch.burnIntensity = spec.burnIntensity;
            scorch.sootRise = 1.9f;
            scorch.emberStrength = spec.emberStrength;
            scorch.Apply();

            // Place localized fire + smoke plume on the damaged roof slope
            var roofFire = (GameObject)PrefabUtility.InstantiatePrefab(roofFirePrefab, effectsRoot);
            roofFire.name = $"Fuego tejado · {spec.name}";
            roofFire.transform.position = new Vector3(
                bounds.center.x + bounds.extents.x * (spec.sideBiasX * 0.85f),
                Mathf.Lerp(bounds.min.y, bounds.max.y, 0.72f),
                bounds.center.z - bounds.extents.z * 0.35f
            );
            roofFire.transform.localScale = Vector3.one * spec.roofScale;

            if (spec.addSmallWallFire)
            {
                var wallFire = (GameObject)PrefabUtility.InstantiatePrefab(smallFirePrefab, effectsRoot);
                wallFire.name = $"Fuego muro · {spec.name}";
                wallFire.transform.position = new Vector3(
                    bounds.center.x + bounds.extents.x * (spec.sideBiasX * 1.15f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, 0.34f),
                    bounds.min.z - 0.12f
                );
                wallFire.transform.localScale = Vector3.one * 0.85f;
            }

            housesUpdated++;
        }

        // Place subtle scorched grass patches in the background meadow
        for (int i = 0; i < GrassPatchLocations.Length; i++)
        {
            Vector3 loc = GrassPatchLocations[i];
            float y = SampleGroundHeight(loc.x, loc.z);
            var patch = (GameObject)PrefabUtility.InstantiatePrefab(grassPatchPrefab, effectsRoot);
            patch.name = $"Pasto quemado · {i + 1:00}";
            patch.transform.position = new Vector3(loc.x, y, loc.z);
            patch.transform.rotation = Quaternion.Euler(0f, (i * 63f + 15f) % 360f, 0f);
            float scale = 0.85f + (i % 3) * 0.15f;
            patch.transform.localScale = Vector3.one * scale;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"RAID_EFFECTS_SCENE_UPDATED {scenePath} houses={housesUpdated} grassPatches={GrassPatchLocations.Length}");
    }

    static float SampleGroundHeight(float x, float z)
    {
        if (Physics.Raycast(new Vector3(x, 12f, z), Vector3.down, out var hit, 25f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        float bestY = float.NegativeInfinity;
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r.enabled) continue;
            string n = r.gameObject.name;
            if (!n.Contains("Pradera") && !n.Contains("Ground") && !n.Contains("Hierba")) continue;
            var b = r.bounds;
            if (x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z && b.max.y > bestY)
                bestY = b.max.y;
        }
        return float.IsNegativeInfinity(bestY) ? VillageTerrainProfile.Height(x) : bestY;
    }

    static void SwapHouseMaterialsToScorched(GameObject house, Material mainScorched, Material roofScorched)
    {
        foreach (var r in house.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                string n = mats[i].name;
                if (n == "MAIN" || n == "MAIN.001" || n == "House_MAIN_Scorched")
                {
                    mats[i] = mainScorched;
                    changed = true;
                }
                else if (n == "ROOFM" || n == "ROOFM.001" || n == "House_ROOFM_Scorched")
                {
                    mats[i] = roofScorched;
                    changed = true;
                }
            }
            if (changed)
            {
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
            }
        }
    }

    static void RestoreHouseMaterials(GameObject house, Material mainOrig, Material roofOrig)
    {
        foreach (var r in house.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                if (mats[i].name == "House_MAIN_Scorched" && mainOrig)
                {
                    mats[i] = mainOrig;
                    changed = true;
                }
                else if (mats[i].name == "House_ROOFM_Scorched" && roofOrig)
                {
                    mats[i] = roofOrig;
                    changed = true;
                }
            }
            if (changed)
            {
                r.sharedMaterials = mats;
                r.SetPropertyBlock(null);
                EditorUtility.SetDirty(r);
            }
        }
    }

    static Bounds ComputeBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
