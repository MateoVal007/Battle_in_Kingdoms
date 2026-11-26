using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VillageFortressLayout
{
    const string RequestPath = "Docs/village-fortress-layout.request";
    const string OnlineScenePath = "Assets/00_Scenes/Pueblo_Online.unity";
    const string FortressName = "Fortaleza del Bosque";
    const string GuardRoot = "Refuerzos del pueblo";
    static double nextPoll;
    static readonly float[] Centers = { 168.7f, 171.9f, 175.1f, 178.3f, 181.5f, 184.7f, 187.9f, 191.1f };

    static VillageFortressLayout() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        nextPoll = EditorApplication.timeSinceStartup + 2;
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        ApplyBoth();
    }

    [MenuItem("Battle in Kingdoms/Preparar fortaleza y refuerzos")]
    public static void ApplyBoth()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var online = EditorSceneManager.OpenScene(OnlineScenePath);
        var door = Object.FindAnyObjectByType<VillageGame>().gate;
        var shelf = GameObject.Find("Base del sello elevado").transform;
        var lintel = GameObject.Find("Base del sello elevado (1)").transform;
        var doorPosition = door.position;
        var doorScale = door.localScale;
        var shelfPosition = shelf.position;
        var shelfScale = shelf.localScale;
        var lintelPosition = lintel.position;
        var lintelScale = lintel.localScale;
        var lintelRotation = lintel.rotation;
        Apply();
        EditorSceneManager.SaveScene(online);

        var local = EditorSceneManager.OpenScene("Assets/00_Scenes/Pueblo_01_Blockout.unity");
        var localDoor = Object.FindAnyObjectByType<VillageGame>().gate;
        localDoor.position = doorPosition;
        localDoor.localScale = doorScale;
        var localShelf = GameObject.Find("Base del sello elevado").transform;
        localShelf.position = shelfPosition;
        localShelf.localScale = shelfScale;
        var localLintel = GameObject.Find("Base del sello elevado (1)");
        if (!localLintel)
            localLintel = Box(localShelf.parent, "Base del sello elevado (1)", lintelPosition, lintelScale, "Stone");
        localLintel.transform.SetPositionAndRotation(lintelPosition, lintelRotation);
        localLintel.transform.localScale = new Vector3(Mathf.Abs(lintelScale.x), Mathf.Abs(lintelScale.y), Mathf.Abs(lintelScale.z));
        Apply();
        EditorSceneManager.SaveScene(local);
        EditorSceneManager.OpenScene(OnlineScenePath);
        File.WriteAllText("Docs/Verification/fortress-layout.txt",
            "Both scenes updated · " + System.DateTime.UtcNow.ToString("s"));
    }

    static void Apply()
    {
        foreach (string name in new[] { FortressName, GuardRoot })
        {
            var previous = GameObject.Find(name);
            if (previous) Object.DestroyImmediate(previous);
        }
        var oldPassage = GameObject.Find("Burbuja · molino y subida del bosque").transform;
        foreach (var child in oldPassage.Cast<Transform>().Where(child => child.position.x >= 169).ToArray())
            Object.DestroyImmediate(child.gameObject);

        var fortress = new GameObject(FortressName).transform;
        float edge = 167.1f;
        for (int i = 0; i < Centers.Length; i++)
        {
            float nextEdge = i == Centers.Length - 1 ? 192.55f : (Centers[i] + Centers[i + 1]) * .5f;
            float top = (i + 1) * 1.4f;
            float x = (edge + nextEdge) * .5f;
            Box(fortress, "Terraza de piedra " + (i + 1), new Vector3(x, (top - .4f) / 2, 0),
                new Vector3(nextEdge - edge, top + .4f, 2.85f), i % 2 == 0 ? "Stone" : "Rock");
            Box(fortress, "Cornisa de la terraza", new Vector3(x, top + .025f, 0),
                new Vector3(nextEdge - edge, .05f, 2.95f), "Wood", false);
            edge = nextEdge;
        }

        for (int i = 0; i < 4; i++)
        {
            float x = 170.3f + i * 6.4f;
            float top = 8.7f + i * 2.8f;
            float width = i == 3 ? 6.25f : 6.4f;
            Box(fortress, "Bóveda sólida", new Vector3(x, top, .5f),
                new Vector3(width, .85f, 4.2f), "Rock");
            if (i > 0)
                Box(fortress, "Frente entre bóvedas", new Vector3(x - 3.2f, top - 1.4f, .5f),
                    new Vector3(.45f, 2.8f, 4.2f), "Rock");
            Box(fortress, "Muro interior", new Vector3(x, (top - .15f) / 2, 2.65f),
                new Vector3(width, top + .5f, .8f), "DarkTimber", false);
            float left = x - width * .5f + .35f;
            float floor = i * 2.8f;
            Box(fortress, "Pilar del arco", new Vector3(left, (top + floor) * .5f, 1.83f),
                new Vector3(.8f, top - floor, .75f), "Stone", false);
            Box(fortress, "Soporte de la antorcha", new Vector3(x, floor + 4.55f, 1.9f),
                new Vector3(.25f, .65f, .45f), "Wood", false);
            Box(fortress, "Llama de la antorcha", new Vector3(x, floor + 5f, 1.85f),
                new Vector3(.24f, .39f, .38f), "Gold", false).transform.rotation = Quaternion.Euler(0, 0, 13);
        }
        Label(fortress, "07 · FORTALEZA DEL BOSQUE", 170.2f, 6.45f);
        Label(fortress, "Suban por las terrazas · cuidado con las dagas", 183, 12.65f);
        Label(fortress, "08 · PUERTA INTERIOR ↓", 191.2f, 14.7f);

        foreach (var chest in Object.FindObjectsByType<VillageChest>())
            if (Mathf.Abs(chest.transform.position.x - 168) < .1f)
                chest.transform.position = new Vector3(165, 3.65f, 0);
        foreach (var zone in Object.FindObjectsByType<VillageZone>())
            if (zone.kind == "Salud" && (Mathf.Abs(zone.transform.position.x - 182) < .1f))
                zone.transform.position = new Vector3(181.5f, 7.55f, 0);
        foreach (var label in Object.FindObjectsByType<TextMesh>())
        {
            if (label.text == "COFRE · ARMADURA" && Mathf.Abs(label.transform.position.x - 168) < .1f)
                label.transform.position = new Vector3(165, 5.35f, -1.8f);
            if (label.text == "Salud" && Mathf.Abs(label.transform.position.x - 182) < .1f)
                label.transform.position = new Vector3(181.5f, 8.7f, -1.8f);
            if (label.text == "08 · PUERTA INTERIOR" && label.transform.position.x < 193)
                label.transform.position = new Vector3(194, 5, -1.8f);
        }
        var stars = Object.FindObjectsByType<VillageStar>()
            .Where(star => star.transform.position.x >= 171 && star.transform.position.x < 190)
            .OrderBy(star => star.transform.position.x).ToArray();
        for (int i = 0; i < stars.Length; i++)
        {
            float t = (i + .5f) / stars.Length;
            int segment = t < .5f ? 2 : 5;
            float arc = t < .5f ? t * 2 : (t - .5f) * 2;
            stars[i].transform.position = new Vector3(Mathf.Lerp(Centers[segment], Centers[segment + 1], arc),
                (segment + 1 + arc) * 1.4f + 1f + Mathf.Sin(arc * Mathf.PI) * .95f, 0);
        }

        foreach (var enemy in Object.FindObjectsByType<VillageEnemy>())
        {
            float x = enemy.transform.position.x;
            if (x < 167.1f || x >= 192.55f || enemy.ranged) continue;
            int deck = Mathf.Clamp(Mathf.FloorToInt((x - 167.1f) / 3.2f), 0, Centers.Length - 1);
            enemy.transform.position = new Vector3(x, (deck + 1) * 1.4f + 1.07f, 0);
        }

        var guards = new GameObject(GuardRoot).transform;
        Guard(guards, 29.6f, 2.27f);
        Guard(guards, 54.4f, 3.97f, dagger: true);
        Guard(guards, 152, 3.37f);
        Guard(guards, 159, 10.2f, archer: true);
        Guard(guards, 175.15f, 5.27f);
        Guard(guards, 184.7f, 9.47f, dagger: true);
        Guard(guards, 221, 2.47f);
        var mountain = Box(guards, "Peñasco del arquero", new Vector3(159.25f, 4.8f, 3.1f),
            new Vector3(3.8f, 8.7f, 2.5f), "Rock", false);
        mountain.transform.rotation = Quaternion.Euler(0, 0, -8);
        Box(guards, "Saliente del arquero", new Vector3(159, 9.13f, .9f),
            new Vector3(2.2f, .34f, 2.3f), "Stone", false);
        Label(guards, "ARQUERO · FLECHAS HACIA ABAJO", 158.9f, 11.7f);
        var entityCount = Object.FindObjectsByType<VillageEnemy>().Length + Object.FindObjectsByType<VillageChest>().Length
            + Object.FindObjectsByType<VillageZone>().Count(zone => !zone.spawnedByChest);
        if (entityCount > 64) throw new System.InvalidOperationException("World snapshot capacity exceeded: " + entityCount);
    }

    static void Guard(Transform parent, float x, float y, bool dagger = false, bool archer = false)
    {
        var guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        guard.name = dagger ? "Guardia de dagas" : archer ? "Arquero del peñasco" : "Patrulla de refuerzo";
        guard.transform.SetParent(parent, false);
        guard.transform.position = new Vector3(x, y, 0);
        guard.transform.localScale = new Vector3(.85f, 1, .85f);
        guard.GetComponent<Renderer>().sharedMaterial = Material(dagger ? "Leaves" : "Danger");
        var enemy = guard.AddComponent<VillageEnemy>();
        enemy.ranged = archer;
        enemy.daggerThrower = dagger;
        enemy.detectionRange = archer ? 14 : dagger ? 13 : 7;
        enemy.attackRange = dagger ? 11 : 1.8f;
        enemy.windup = dagger ? .64f : .45f;
        enemy.cooldown = dagger ? 1.65f : 1.3f;
        enemy.damage = dagger ? 14 : 15;
        enemy.shieldHealth = dagger ? 60 : 0;
        enemy.health = dagger ? 75 : 60;
        enemy.patrolRadius = dagger ? 1.3f : 2.15f;
    }

    static Material Material(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_" + name + ".mat");

    internal static GameObject Box(Transform root, string name, Vector3 position, Vector3 size, string material, bool solid = true)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root, false);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = Material(material);
        if (!solid) Object.DestroyImmediate(box.GetComponent<Collider>());
        return box;
    }

    static void Label(Transform root, string message, float x, float y)
    {
        var obj = new GameObject(message);
        obj.transform.SetParent(root, false);
        obj.transform.position = new Vector3(x, y, -1.8f);
        var label = obj.AddComponent<TextMesh>();
        label.text = message;
        label.fontSize = 64;
        label.characterSize = .047f;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(1, .92f, .68f);
    }
}

