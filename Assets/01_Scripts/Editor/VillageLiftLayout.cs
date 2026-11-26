using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VillageLiftLayout
{
    const string RequestPath = "Docs/village-lift-layout.request";
    const string RootName = "Burbuja · molino y subida del bosque";
    static double nextPoll;

    static VillageLiftLayout() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        nextPoll = EditorApplication.timeSinceStartup + 2;
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        ApplyBoth();
    }

    [MenuItem("Battle in Kingdoms/Preparar recorrido de burbuja")]
    public static void ApplyBoth()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var path in new[] { "Pueblo_01_Blockout", "Pueblo_Online" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/00_Scenes/" + path + ".unity");
            Apply();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        File.WriteAllText("Docs/Verification/lift-layout.txt", "Both scenes updated · " + System.DateTime.UtcNow.ToString("s"));
    }

    static void Apply()
    {
        var old = GameObject.Find(RootName);
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject(RootName).transform;
        var game = Object.FindAnyObjectByType<VillageGame>();

        var oldSeal = GameObject.Find("Sello escudo");
        if (oldSeal) Object.DestroyImmediate(oldSeal);
        var oldMagePad = GameObject.Find("Sello magia");
        if (oldMagePad) Object.DestroyImmediate(oldMagePad);
        Box(root, "Base del sello elevado", new Vector3(140.05f, 2.25f, 0),
            new Vector3(2.9f, 4.5f, 2.8f), "Stone");
        game.liftSeal = Box(root, "Sello elevado · aterriza aquí", new Vector3(140.05f, 4.575f, 0),
            new Vector3(2.7f, .15f, 2.8f), "Blue").transform;
        Box(root, "Marca de lanzamiento", new Vector3(136.25f, .14f, 0),
            new Vector3(3.5f, .06f, 2.1f), "Mage", false);

        foreach (var label in Object.FindObjectsByType<TextMesh>())
        {
            if (label.text.StartsWith("Guerrero: piso AZUL") || label.text.StartsWith("Guerrero en AZUL"))
            {
                label.text = "06 · BURBUJA COOPERATIVA\nG pide · el otro acepta con G\nEleva al guerrero y suéltalo en el sello azul";
                label.transform.position = new Vector3(134, 7.5f, -1.8f);
                label.characterSize = .047f;
            }
            if (label.text.Contains("G + W")) label.text = label.text.Replace("G + W", "L + W");
            if (label.text == "COFRE · ARMADURA" && Mathf.Abs(label.transform.position.x - 140) < .1f)
                label.transform.position = new Vector3(130.5f, 2.7f, -1.8f);
            if (label.text == "Salud" && Mathf.Abs(label.transform.position.x - 180) < .1f)
                label.transform.position = new Vector3(182, 6.1f, -1.8f);
        }
        foreach (var chest in Object.FindObjectsByType<VillageChest>())
            if (Mathf.Abs(chest.transform.position.x - 140) < .1f)
                chest.transform.position = new Vector3(130.5f, .85f, 0);

        var oldRoute = GameObject.Find("Diseño v5 · rutas de salto");
        foreach (Transform child in oldRoute.transform.Cast<Transform>().ToArray())
            if (child.position.x >= 171 && child.position.x < 190)
                Object.DestroyImmediate(child.gameObject);

        // Open cutaway in front, with a real collision ceiling and a high exit.
        Box(root, "Muro de fondo · pasaje", new Vector3(181, 4.55f, 2.6f),
            new Vector3(21, 9.1f, .6f), "DarkTimber", false);
        Box(root, "Techo sólido del pasaje", new Vector3(180.7f, 9.85f, .35f),
            new Vector3(20.8f, .7f, 3.6f), "Stone");
        Box(root, "Viga de entrada", new Vector3(170.7f, 6.2f, 1.65f),
            new Vector3(.65f, 7, .65f), "Wood", false);

        var stairs = new[]
        {
            new Vector3(174, 1.85f, 2.5f),
            new Vector3(178, 3.25f, 2.6f),
            new Vector3(182, 4.65f, 2.8f),
            new Vector3(188.35f, 6.05f, 5.3f)
        };
        foreach (var stair in stairs)
        {
            Box(root, "Escalón sólido", new Vector3(stair.x, (stair.y - .4f) / 2, 0),
                new Vector3(stair.z, stair.y + .4f, 2.7f), "Stone");
            Box(root, "Borde de escalón", new Vector3(stair.x, stair.y + .018f, 0),
                new Vector3(stair.z, .036f, 2.7f), "Gold", false);
        }
        Label(root, "07 · PASAJE DEL BOSQUE", 174, 8.3f);
        Label(root, "Suban por los escalones\no practiquen con la burbuja", 178, 7.0f);

        foreach (var zone in Object.FindObjectsByType<VillageZone>())
            if (zone.kind == "Salud" && Mathf.Abs(zone.transform.position.x - 180) < .1f)
                zone.transform.position = new Vector3(182, 5.2f, 0);
        var millPatrol = GameObject.Find("Patrulla v6 · 135");
        if (millPatrol) millPatrol.transform.position = new Vector3(131.8f, 1.15f, 0);
        var patrol = GameObject.Find("Patrulla v6 · 190.5");
        if (patrol) patrol.transform.position = new Vector3(192.1f, 1.15f, 0);

        var stars = Object.FindObjectsByType<VillageStar>()
            .Where(star => star.transform.position.x >= 171 && star.transform.position.x <= 190)
            .OrderBy(star => star.transform.position.x).ToArray();
        for (int i = 0; i < stars.Length; i++)
        {
            int segment = Mathf.Min(2, i * 3 / stars.Length);
            float start = segment * stars.Length / 3f;
            float t = Mathf.Clamp01((i + .5f - start) / (stars.Length / 3f));
            var from = stairs[segment];
            var to = stairs[segment + 1];
            stars[i].transform.position = new Vector3(Mathf.Lerp(from.x + .6f, to.x - .6f, t),
                Mathf.Lerp(from.y, to.y, t) + .95f + Mathf.Sin(t * Mathf.PI) * 1.1f, 0);
        }
    }

    static GameObject Box(Transform root, string name, Vector3 position, Vector3 size, string material, bool solid = true)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root, false);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_" + material + ".mat");
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

