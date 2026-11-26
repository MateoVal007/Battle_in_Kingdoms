using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static VillageFortressLayout;

[InitializeOnLoad]
public static class VillageExplorationLayout
{
    const string Request = "Docs/village-exploration-layout.request";
    const string OnlineScene = "Assets/00_Scenes/Pueblo_Online.unity";
    const string RootName = "Exploración · salas y rutas altas";
    static double nextPoll;

    static VillageExplorationLayout() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        nextPoll = EditorApplication.timeSinceStartup + 2;
        if (!File.Exists(Request)) return;
        File.Delete(Request);
        ApplyBoth();
    }

    [MenuItem("Battle in Kingdoms/Preparar salas y rutas altas")]
    public static void ApplyBoth()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var path in new[] { "Assets/00_Scenes/Pueblo_01_Blockout.unity", OnlineScene })
        {
            var scene = EditorSceneManager.OpenScene(path);
            Apply();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Docs/Verification/exploration-layout.txt", "Both scenes updated · " + System.DateTime.UtcNow.ToString("s"));
    }

    static void Apply()
    {
        var old = GameObject.Find(RootName);
        if (old) Object.DestroyImmediate(old);
        var fortress = GameObject.Find("Fortaleza del Bosque");
        if (fortress) Object.DestroyImmediate(fortress);
        var root = new GameObject(RootName).transform;

        var v5 = GameObject.Find("Diseño v5 · rutas de salto").transform;
        RemovePlatforms(v5, 58, 62, 123, 127);
        var v6 = GameObject.Find("Revisión v6 · patrullas y camino real").transform;
        RemovePlatforms(v6, 82.5f, 86.5f, 90.5f, 94.5f, 98.5f, 104.5f,
            196, 200, 204, 208, 212, 220, 224, 228, 232, 236);

        Deck(root, "Tejado continuo", 56.5f, 63.7f, 4.3f, "Wood");
        Deck(root, "Puente alto del arroyo", 81, 93.2f, 6.4f, "Wood");
        Box(root, "Puntal del puente", new Vector3(89.6f, 2.3f, 1.2f), new Vector3(.27f, 7.5f, .27f), "Wood", false);
        Deck(root, "Galería baja del Molino", 122, 129, 1.4f, "Wood");

        Ramp(root, "Camino_real_rampa", 195, 198.8f, .12f, 1.85f);
        Floor(root, "Muralla del camino real", 198.8f, 210, 1.85f);
        Ramp(root, "Patio_rampa", 218.5f, 223, .12f, 2.45f);
        Floor(root, "Patio de combate", 223, 232.4f, 2.45f);
        MoveChest(204, 204, 1.85f);
        MoveChest(228, 228, 2.45f);

        var rooms = new GameObject("Fortaleza · cuerpo de guardia").transform;
        rooms.SetParent(root, false);
        Ramp(rooms, "Fortaleza_entrada", 166.25f, 170, .12f, 1.6f);
        Floor(rooms, "Sala baja", 170, 175.4f, 1.6f);
        Floor(rooms, "Umbral del corredor", 175.4f, 180, 3.1f);
        Ramp(rooms, "Fortaleza_corredor", 180, 184, 3.1f, 4.6f);
        Floor(rooms, "Sala alta", 184, 192.55f, 4.6f);
        Vault(rooms, 167.1f, 176.9f, 9.25f, 1.6f);
        Vault(rooms, 176.9f, 182, 6.6f, 3.1f);
        Vault(rooms, 182, 192.55f, 10.25f, 4.6f);
        JoinVaults(rooms, 176.9f, 6.6f, 9.25f);
        JoinVaults(rooms, 182, 6.6f, 10.25f);

        var template = Object.FindObjectsByType<VillageChest>().First(chest => chest.reward == "Armadura");
        Secret(root, template, "Desván de la plaza", 116.7f, 121.55f, 5.4f, "Armadura");
        Secret(rooms, template, "Reserva del cuerpo de guardia", 171.9f, 176.6f, 6.35f, "Vida extra");
        Box(rooms, "Cierre del archivo", new Vector3(176.75f, 7.54f, .05f),
            new Vector3(.3f, 3.92f, 2.9f), "Rock");

        foreach (var zone in Object.FindObjectsByType<VillageZone>())
            if (zone.kind == "Salud" && Mathf.Abs(zone.transform.position.x - 181.5f) < .1f)
                zone.transform.position = new Vector3(186.5f, 5.15f, 0);
        foreach (var label in Object.FindObjectsByType<TextMesh>())
        {
            float x = label.transform.position.x;
            if (label.text == "Salud" && Mathf.Abs(x - 181.5f) < .1f)
                label.transform.position = new Vector3(186.5f, 6.3f, -1.8f);
            if (label.text.StartsWith("04 · ARROYO"))
                label.text = "04 · ARROYO";
        }
        foreach (var enemy in Object.FindObjectsByType<VillageEnemy>())
        {
            float x = enemy.transform.position.x;
            if (enemy.ranged) continue;
            float floor;
            if (x >= 170 && x < 192.55f)
                floor = x < 175.4f ? 1.6f : x < 180 ? 3.1f : x < 184 ? 3.1f + (x - 180) * .375f : 4.6f;
            else if (x >= 198.8f && x < 210) floor = 1.85f;
            else if (x >= 218.5f && x < 232.4f)
                floor = Mathf.Lerp(.12f, 2.45f, Mathf.InverseLerp(218.5f, 223, x));
            else continue;
            enemy.transform.position = new Vector3(x, floor + 1.07f, 0);
        }

        // Reuse the existing stars so their IDs and the shared level total remain stable.
        Stars(57.5f, 64, (i, n) => new Vector2(57.2f + i * .9f, 5.35f));
        Stars(81.8f, 92.9f, (i, n) => new Vector2(83 + i * .91f, 7.45f), star => star.transform.position.y > 5);
        Stars(115, 119.9f, (i, n) => i < 2 ? new Vector2(117.7f + i * .8f, 6.45f) : new Vector2(114.7f + (i - 2) * 1.2f, 1.17f));
        Stars(122.8f, 127.2f, (i, n) => new Vector2(123 + i * .9f, 2.45f));
        Stars(171.8f, 190, (i, n) => i < 3
            ? new Vector2(172.6f + i * .73f, 7.4f)
            : new Vector2(184.6f + (i - 3) * .84f, 5.65f));
        Stars(194.9f, 211, (i, n) => new Vector2(199.6f + i * .87f, 2.9f));
        Stars(223.9f, 237, (i, n) => i < n / 2
            ? new Vector2(223.7f + i * .83f, 3.5f)
            : new Vector2(233.4f + (i - n / 2) * .9f, 1.17f));
    }

    static void RemovePlatforms(Transform parent, params float[] centers)
    {
        foreach (var child in parent.Cast<Transform>().ToArray())
            if (centers.Any(x => Mathf.Abs(x - child.position.x) < .04f)
                && (child.name.StartsWith("Plataforma") || child.name.StartsWith("Borde")
                    || child.name.StartsWith("Terraplén") || child.name.StartsWith("Base sólida")))
                Object.DestroyImmediate(child.gameObject);
            else if (child.name.StartsWith("Puntal") && centers.Any(x => Mathf.Abs(x - child.position.x) < 1.39f))
                Object.DestroyImmediate(child.gameObject);
    }

    static void Stars(float start, float end, System.Func<int, int, Vector2> position,
        System.Func<VillageStar, bool> filter = null)
    {
        var stars = Object.FindObjectsByType<VillageStar>()
            .Where(star => star.transform.position.x >= start && star.transform.position.x <= end && (filter == null || filter(star)))
            .OrderBy(star => star.id).ToArray();
        for (int i = 0; i < stars.Length; i++) stars[i].transform.position = position(i, stars.Length);
    }

    static void MoveChest(float x, float destination, float floor)
    {
        var chest = Object.FindObjectsByType<VillageChest>()
            .First(c => c.reward == "Armadura" && Mathf.Abs(c.transform.position.x - x) < .1f);
        chest.transform.position = new Vector3(destination, floor + .85f, 0);
        foreach (var label in Object.FindObjectsByType<TextMesh>())
            if (label.text == "COFRE · ARMADURA" && Mathf.Abs(label.transform.position.x - x) < .1f)
                label.transform.position = new Vector3(destination, floor + 2.5f, -1.8f);
    }

    static void Floor(Transform parent, string name, float left, float right, float top) =>
        Box(parent, name, new Vector3((left + right) / 2, (top - .45f) / 2, 0),
            new Vector3(right - left, top + .45f, 3), "Stone");

    static void Deck(Transform parent, string name, float left, float right, float top, string mat) =>
        Box(parent, name, new Vector3((left + right) / 2, top - .2f, 0),
            new Vector3(right - left, .4f, 2.8f), mat);

    static void Ramp(Transform parent, string name, float left, float right, float low, float high) =>
        VillageDesignRevision.Wedge(name, left, right, low, high, -.4f, -.4f,
            AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_Stone.mat"), parent);

    static void Vault(Transform parent, float left, float right, float underside, float floor)
    {
        float x = (left + right) / 2;
        Box(parent, "Techo de la sala", new Vector3(x, underside + .35f, .35f),
            new Vector3(right - left, .7f, 3.7f), "Rock");
        Box(parent, "Mampostería interior", new Vector3(x, underside * .5f, 2.55f),
            new Vector3(right - left, underside, .75f), "DarkTimber", false);
        foreach (float pillar in new[] { left + .45f, right - .45f })
            Box(parent, "Pilastra", new Vector3(pillar, (floor + underside) / 2, 1.91f),
                new Vector3(.67f, underside - floor, .75f), "Stone", false);
        Box(parent, "Antorcha", new Vector3(x, floor + 2.9f, 1.9f),
            new Vector3(.22f, .36f, .27f), "Gold", false).transform.rotation = Quaternion.Euler(0, 0, 15);
    }

    static void JoinVaults(Transform parent, float x, float bottom, float top) =>
        Box(parent, "Dintel del corredor", new Vector3(x, (bottom + top + .7f) * .5f, .35f),
            new Vector3(.4f, top + .7f - bottom, 3.7f), "Stone");

    static void Secret(Transform parent, VillageChest template, string name, float left, float right, float top, string reward)
    {
        var room = new GameObject(name).transform;
        room.SetParent(parent, false);
        float x = (left + right) / 2;
        Deck(room, "Cornisa de acceso", left, right, top, "Stone");
        Box(room, "Pared del fondo", new Vector3(x, top + 1.4f, 1.82f),
            new Vector3(right - left, 2.8f, .45f), "Wood", false);
        Box(room, "Herraje de la entrada", new Vector3(left + .12f, top + .65f, -1.51f),
            new Vector3(.14f, .8f, .12f), "Gold", false);
        var chest = Object.Instantiate(template, room);
        chest.name = "Cofre oculto · " + reward;
        chest.transform.position = new Vector3(right - 1.05f, top + .85f, 0);
        chest.reward = reward;
        chest.keepOnRetry = true;
        var cover = Box(room, "Fachada del escondite", new Vector3(x + .33f, top + 1.46f, -1.42f),
            new Vector3(right - left - .66f, 2.74f, .17f), "DarkTimber", false).AddComponent<VillageSecretCover>();
        cover.chest = chest;
        cover.left = left;
        cover.right = right;
        cover.floor = top;
    }
}

