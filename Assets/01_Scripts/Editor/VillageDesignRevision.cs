using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class VillageDesignRevision
{
    static double next;
    static VillageDesignRevision(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;next=EditorApplication.timeSinceStartup+2;if(File.Exists("Docs/village-design.request")){File.Delete("Docs/village-design.request");ApplyBoth();}}
    [MenuItem("Battle in Kingdoms/Aplicar revisión de caminos y habilidades")]
    public static void ApplyBoth()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        foreach(var path in new[]{"Assets/00_Scenes/Pueblo_01_Blockout.unity","Assets/00_Scenes/Pueblo_Online.unity"})
        {var scene=EditorSceneManager.OpenScene(path);ApplyScene();ApplySolidLayout();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        AssetDatabase.SaveAssets();File.WriteAllText("Docs/Verification/design-build.txt","Applied to local and online scenes · "+System.DateTime.Now.ToString("s"));
    }
    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_"+name+".mat");
    public static void ApplyScene()
    {
        CleanLabels();
        if(GameObject.Find("Diseño v2 · laderas y caminos")){
            SetPlatform(31,3.8f);SetPlatform(147,3.2f);
            var shooterExisting=GameObject.Find("Tirador elevado · escudo superior");if(shooterExisting)shooterExisting.transform.position=new Vector3(44.2f,4.45f,0);
            return;}
        if(!AssetDatabase.IsValidFolder("Assets/04_LevelGeometry"))AssetDatabase.CreateFolder("Assets","04_LevelGeometry");
        var marker=new GameObject("Diseño v2 · laderas y caminos");
        int[] sectorIds={1,2,6,7};
        var sectors=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for(int j=0;j<sectorIds.Length;j++)
        {
            int id=sectorIds[j];var sector=sectors.First(o=>o.name.StartsWith((id+1).ToString("00")+" ·"));
            foreach(Transform child in sector.transform.Cast<Transform>().ToArray())if(child.name=="Terreno"||child.name=="Hierba")Object.DestroyImmediate(child.gameObject);
            var points=VillageTerrainProfile.Sections[j];
            for(int i=0;i<points.Length-1;i++)
            {
                var a=points[i];var b=points[i+1];
                Wedge("Tierra "+id+"_"+i,a.x,b.x,a.y,b.y,-5,-5,Mat("Earth"),sector.transform);
                Wedge("Ladera "+id+"_"+i,a.x,b.x,a.y+.12f,b.y+.12f,a.y,b.y,Mat("Grass"),sector.transform);
            }
        }
        // Clear the old low ceiling-like platforms, replacing them with readable upper routes.
        SetPlatform(27,1.6f);SetPlatform(31,3.8f);SetPlatform(36,5.5f);SetPlatform(43,3.2f);
        SetPlatform(54,3.2f);SetPlatform(59,5.3f);SetPlatform(66,5.3f);SetPlatform(70,2.4f);
        SetPlatform(147,3.2f);SetPlatform(151,4.4f);SetPlatform(156,6.4f);SetPlatform(163,4.2f);
        SetPlatform(173,3.4f);SetPlatform(178,5.4f);
        Wedge("Pasarela diagonal de tejados",60.5f,64,5.525f,6.425f,5.225f,6.125f,Mat("Wood"),marker.transform);
        // Recessed paths retain hazards and rewards at their new ground height.
        foreach(var z in Object.FindObjectsByType<VillageZone>())
        {
            float x=z.transform.position.x;
            if(z.kind=="Espinos")Shift(z.transform,VillageTerrainProfile.Height(x));
            if(x>33&&x<39&&z.kind=="Armadura")Shift(z.transform,1.5f);
            if(x>60&&x<67&&z.kind=="Hielo")Shift(z.transform,1.1f);
            if(x>153&&x<158&&z.kind=="Fuego")Shift(z.transform,2.2f);
            if(x>176&&x<180&&z.kind=="Salud")Shift(z.transform,2.4f);
        }
        foreach(var chest in Object.FindObjectsByType<VillageChest>())Shift(chest.transform,chest.transform.position.x==66?1.1f:VillageTerrainProfile.Height(chest.transform.position.x));
        foreach(var e in Object.FindObjectsByType<VillageEnemy>())Shift(e.transform,VillageTerrainProfile.Height(e.transform.position.x));
        foreach(var t in Object.FindObjectsByType<Transform>())
        {
            if(t.name=="Tallo flor"||t.name=="Flor"||t.name=="Piedra"||t.name=="Arbusto")Shift(t,VillageTerrainProfile.Height(t.position.x));
        }
        Label("02 · LADERAS DEL HUERTO",35,9,marker.transform);
        Label("03 · CALLE BAJA / TEJADOS",59,9,marker.transform);
        Label("07 · COLINA Y HONDONADA",155,10,marker.transform);
        // Raised shooter provides actual incoming attacks from above.
        var shooter=GameObject.CreatePrimitive(PrimitiveType.Capsule);shooter.name="Tirador elevado · escudo superior";
        shooter.transform.SetParent(marker.transform);shooter.transform.position=new Vector3(44.2f,4.45f,0);shooter.transform.localScale=new Vector3(.8f,1,.8f);
        shooter.GetComponent<Renderer>().sharedMaterial=Mat("Danger");shooter.AddComponent<VillageEnemy>().ranged=true;
    }
    static void Shift(Transform t,float dy){t.position+=Vector3.up*dy;}
    public static void ApplySolidLayout()
    {
        var existing=GameObject.Find("Diseño v3 · plataformas sólidas");
        var root=existing?existing.transform:new GameObject("Diseño v3 · plataformas sólidas").transform;
        foreach(Transform child in root.Cast<Transform>().ToArray())if(child.name=="Base sólida · salto obligatorio")Object.DestroyImmediate(child.gameObject);
        SetPlatform(27,2);SetPlatform(31,4.2f);
        MovePlatform(147,145,1.6f);MovePlatform(151,149,3.6f);MovePlatform(156,154,5.6f);MovePlatform(163,161,5.8f);
        foreach(var z in Object.FindObjectsByType<VillageZone>())if(z.kind=="Fuego"&&Mathf.Abs(z.transform.position.x-156)<.1f)z.transform.position=new Vector3(154,6.4f,0);
        foreach(var t in Object.FindObjectsByType<Transform>())
        {
            bool platform=t.name=="Plataforma de madera";bool border=t.name=="Borde claro";if(!platform&&!border)continue;
            float x=t.position.x;
            if(Mathf.Abs(x-78)<.01f&&t.position.y<3){Object.DestroyImmediate(t.gameObject);continue;}
            if(Mathf.Abs(x-70)<.01f)t.localScale=new Vector3(2,t.localScale.y,t.localScale.z);
            if(Mathf.Abs(x-72)<.01f)t.localScale=new Vector3(1.5f,t.localScale.y,t.localScale.z);
            if((Mathf.Abs(x-82)<.01f||Mathf.Abs(x-88)<.01f)&&t.position.y>4)t.position=new Vector3(x,6.3f+(border?.24f:0),0);
            if(Mathf.Abs(x-83)<.01f){t.position=new Vector3(x,1.4f+(border?.24f:0),0);t.localScale=new Vector3(3,t.localScale.y,t.localScale.z);}
            if(Mathf.Abs(x-88)<.01f&&t.position.y<3){t.position=new Vector3(x,.8f+(border?.24f:0),0);t.localScale=new Vector3(2,t.localScale.y,t.localScale.z);}
            if((Mathf.Abs(x-87)<.01f||Mathf.Abs(x-86)<.01f||Mathf.Abs(x-86.5f)<.01f)&&t.position.y<0){t.position=new Vector3(86.5f,t.position.y,0);t.localScale=new Vector3(1.4f,t.localScale.y,t.localScale.z);}
        }
        if(!Object.FindObjectsByType<Transform>().Any(t=>t.name=="Plataforma de madera"&&Mathf.Abs(t.position.x-68)<.1f)){var access=GameObject.CreatePrimitive(PrimitiveType.Cube);access.name="Plataforma de madera";access.transform.SetParent(root);access.transform.position=new Vector3(68,1,0);access.transform.localScale=new Vector3(1.8f,.45f,2.5f);access.GetComponent<Renderer>().sharedMaterial=Mat("Wood");}
        if(!Object.FindObjectsByType<Transform>().Any(t=>t.name=="Plataforma de madera"&&Mathf.Abs(t.position.x-78)<.1f))
        {var bridge=GameObject.CreatePrimitive(PrimitiveType.Cube);bridge.name="Plataforma de madera";bridge.transform.SetParent(root);bridge.transform.position=new Vector3(78,5.4f,0);bridge.transform.localScale=new Vector3(2,.45f,2.5f);bridge.GetComponent<Renderer>().sharedMaterial=Mat("Wood");}
        foreach(var z in Object.FindObjectsByType<VillageZone>())if(z.kind=="Salud"&&Mathf.Abs(z.transform.position.x-88)<.1f)z.transform.position=new Vector3(88,7.1f,0);
        foreach(var t in Object.FindObjectsByType<Transform>())
        {
            if(t.name!="Plataforma de madera")continue;
            var box=t.GetComponent<BoxCollider>();if(!box)continue;
            float floor=t.position.x>79&&t.position.x<89?-2:VillageTerrainProfile.Height(t.position.x)+.12f;
            float bottom=t.position.y-t.localScale.y*.5f;
            if(bottom-floor>=2.4f)continue;
            var baseBlock=GameObject.CreatePrimitive(PrimitiveType.Cube);baseBlock.name="Base sólida · salto obligatorio";baseBlock.transform.SetParent(root);
            baseBlock.transform.position=new Vector3(t.position.x,(floor+bottom)*.5f,0);baseBlock.transform.localScale=new Vector3(t.localScale.x,Mathf.Max(.05f,bottom-floor),t.localScale.z);
            baseBlock.GetComponent<Renderer>().sharedMaterial=Mat("Stone");
        }
        var game=Object.FindAnyObjectByType<VillageGame>();game.warrior.armor=0;game.warrior.hasEquipment=false;
        var starter=Object.FindObjectsByType<VillageChest>().First(c=>Mathf.Abs(c.transform.position.x-6)<.1f);starter.reward="Equipo";starter.name="Cofre inicial · espada escudo armadura";
        if(!Object.FindObjectsByType<VillageChest>().Any(c=>Mathf.Abs(c.transform.position.x-20)<.1f))
        {var health=Object.Instantiate(starter.gameObject,starter.transform.parent);health.name="Cofre de salud";health.transform.position=new Vector3(20,.85f,0);health.GetComponent<VillageChest>().reward="Salud";}
        foreach(var label in Object.FindObjectsByType<TextMesh>())if(Mathf.Abs(label.transform.position.x-6)<.1f&&label.text.StartsWith("COFRE"))label.text="F · PUÑETAZO\nESPADA + ESCUDO + ARMADURA";
        if(!GameObject.Find("RUTA BAJA: salta desde la orilla"))Label("RUTA BAJA: salta desde la orilla",77,2.7f,root);
    }
    static void MovePlatform(float oldX,float newX,float y){foreach(var t in Object.FindObjectsByType<Transform>())if(Mathf.Abs(t.position.x-oldX)<.01f&&(t.name=="Plataforma de madera"||t.name=="Borde claro"))t.position=new Vector3(newX,y+(t.name=="Borde claro"?.24f:0),0);}
    static void CleanLabels(){foreach(var label in Object.FindObjectsByType<TextMesh>())if(label.text=="02 · HUERTOS"||label.text=="03 · TEJADOS"||label.text=="07 · BOSQUE"||label.text.StartsWith("ARRIBA: libros"))Object.DestroyImmediate(label.gameObject);}
    static void SetPlatform(float x,float y)
    {
        foreach(var t in Object.FindObjectsByType<Transform>())if(Mathf.Abs(t.position.x-x)<.01f)
        {if(t.name=="Plataforma de madera")t.position=new Vector3(x,y,t.position.z);if(t.name=="Borde claro")t.position=new Vector3(x,y+.24f,t.position.z);}
    }
    static void Label(string text,float x,float y,Transform parent){var o=new GameObject(text);o.transform.SetParent(parent);o.transform.position=new Vector3(x,y,-1.8f);var label=o.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=.065f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.12f,.18f,.23f);}
    internal static void Wedge(string name,float x0,float x1,float top0,float top1,float bottom0,float bottom1,Material material,Transform parent)
    {
        string path="Assets/04_LevelGeometry/"+name.Replace(' ','_')+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.vertices=new[]{new Vector3(x0,bottom0,-1.5f),new Vector3(x1,bottom1,-1.5f),new Vector3(x0,top0,-1.5f),new Vector3(x1,top1,-1.5f),new Vector3(x0,bottom0,1.5f),new Vector3(x1,bottom1,1.5f),new Vector3(x0,top0,1.5f),new Vector3(x1,top1,1.5f)};
        mesh.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6,2,6,3,3,6,7,0,1,4,1,5,4,0,4,2,2,4,6,1,3,5,3,7,5};mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;o.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
}
