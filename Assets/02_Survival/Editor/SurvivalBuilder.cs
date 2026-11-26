using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Fusion;
using Object=UnityEngine.Object;

namespace Kingdoms.Survival.Editor
{
    public static class SurvivalBuilder
    {
        public const string ScenePath="Assets/00_Scenes/Supervivencia_02.unity";
        const string Folder="Assets/02_Survival/";
        const string Work="C:/Users/MAQ/Documents/ChatGPT/Juego final/work/survival-level2/";
        static readonly List<NavMeshBuildSource> sources=new List<NavMeshBuildSource>();
        static readonly List<SurvivalPickup> pickups=new List<SurvivalPickup>();
        static SurvivalArena arena;
        static Transform geometry,artRoot;
        static Material floor,rock,wood,green,steel,gold,blue,violet;

        [MenuItem("Battle in Kingdoms/Nivel 2/Crear arena de supervivencia")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder+"Art");Directory.CreateDirectory(Work);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);sources.Clear();pickups.Clear();
            floor=Mat("Adoquin",new Color(.70f,.66f,.55f));rock=Mat("Piedra",new Color(.34f,.39f,.40f));wood=Mat("Madera",new Color(.34f,.23f,.15f));green=Mat("Pradera",new Color(.32f,.46f,.19f));steel=Mat("Acero",new Color(.48f,.59f,.66f));gold=Mat("Oro",new Color(1,.62f,.12f));blue=Mat("Jugador1",new Color(.16f,.55f,1));violet=Mat("Jugador2",new Color(.63f,.24f,1));
            floor.shader=Shader.Find("Kingdoms/PlazaAdoquin");floor.SetColor("_BaseColor",new Color(.60f,.57f,.49f));floor.SetFloat("_TileScale",.9f);
            arena=new GameObject("Nivel 2 · Defensa de la plaza").AddComponent<SurvivalArena>();
            arena.stone=rock;arena.steel=steel;arena.amber=gold;arena.warning=Mat("Aviso",new Color(1,.32f,.08f));
            arena.basicSpell=Mat("Magia normal",new Color(.68f,.22f,1));arena.basicSpell.EnableKeyword("_EMISSION");arena.basicSpell.SetColor("_EmissionColor",new Color(.7f,.2f,1)*2);
            arena.protection=AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/Village_ProtectionField.mat");
            geometry=new GameObject("Geometría jugable · plaza y primer piso").transform;
            artRoot=new GameObject("Ambiente · pueblo fortificado").transform;
            Box("Terreno exterior",new Vector3(0,-.7f,-12),new Vector3(120,.5f,120),green,false);
            Box("Cimiento de las casas",new Vector3(0,1.7f,26),new Vector3(62,3.8f,12),rock,false);
            Box("Suelo de la plaza",new Vector3(0,-.3f,-17),new Vector3(60,.6f,74),floor,true);
            Box("Base maciza de la terraza",new Vector3(0,2,16),new Vector3(60,4,8),rock,true);
            Box("Piso superior",new Vector3(0,4.025f,16),new Vector3(60,.05f,8),floor,false);
            Box("Muralla oeste",new Vector3(-30.5f,1.3f,-17),new Vector3(1,2.6f,75),rock,true);
            Box("Muralla este",new Vector3(30.5f,1.3f,-17),new Vector3(1,2.6f,75),rock,true);
            Box("Muralla norte",new Vector3(0,5.2f,20.5f),new Vector3(62,2.4f,1),rock,true);
            Box("Límite sur lejano",new Vector3(0,2.5f,-55),new Vector3(62,5,1),rock,true).GetComponent<Renderer>().enabled=false;
            Stairs(-18);Stairs(18);SideFloors();
            foreach(float x in new[]{-23f,-14,-6,6,14,23})
            {
                Box("Pilar del primer piso",new Vector3(x,2,11.7f),new Vector3(.65f,4,.65f),wood,false);
                Box("Poste baranda",new Vector3(x,4.6f,12.15f),new Vector3(.14f,1.2f,.14f),wood,false);
            }
            foreach(var segment in new[]{new Vector2(-23,-20.5f),new Vector2(-15.5f,-3),new Vector2(3,15.5f),new Vector2(20.5f,23)})
                Box("Baranda de la terraza",new Vector3((segment.x+segment.y)*.5f,4.8f,12.15f),new Vector3(segment.y-segment.x,.16f,.14f),wood,false);
            foreach(var pos in new[]{new Vector3(-8,0,2),new Vector3(10,0,5),new Vector3(-11,0,-9),new Vector3(12,0,-11),new Vector3(-16,0,-30),new Vector3(17,0,-39)})
            {
                var b=Box("Cobertura de piedra",pos+Vector3.up*.65f,new Vector3(2.5f,1.3f,1.8f),rock,true);
                PlaceKit("Decoracion/Crate1",pos,new Vector3(2.35f,1.4f,1.7f),15);
                b.GetComponent<Renderer>().enabled=false;
            }
            Circle("Ruedo central",new Vector3(0,.025f,0),8,floor);
            Box("Fuente · cobertura central",new Vector3(0,.55f,1),new Vector3(3,1.1f,3),rock,true).GetComponent<Renderer>().enabled=false;
            var basin=Circle("Pila de piedra",new Vector3(0,.48f,1),1.55f,rock);basin.transform.localScale=new Vector3(3.1f,.48f,3.1f);
            Circle("Agua de la fuente",new Vector3(0,.975f,1),1.17f,blue);
            for(int i=0;i<16;i++)
            {float a=i*Mathf.PI/8;Box("Adoquín del ruedo",new Vector3(Mathf.Cos(a)*8.5f,.035f,Mathf.Sin(a)*8.5f),new Vector3(.9f,.05f,.4f),i%2==0?wood:rock,false).transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
            arena.entrances=new[]{new Vector3(-28,0,-48),new Vector3(28,0,-40),new Vector3(-28,0,2),new Vector3(28,0,10),new Vector3(-12,0,-49),new Vector3(12,0,-49)};
            for(int i=0;i<arena.entrances.Length;i++){Circle("Entrada de horda "+(i+1),arena.entrances[i]+Vector3.up*.05f,1.5f,gold);}
            var gate=Box("Portón de salida · se abre tras el gigante",new Vector3(0,5.4f,19.6f),new Vector3(4,2.8f,.5f),wood,true);arena.exitGate=gate.transform;
            WorldLabel("SALIDA",new Vector3(0,7.3f,19.3f),.32f);
            WorldLabel("03 · TERRAZA",new Vector3(0,7.6f,14),.15f);
            WorldLabel("02 · GALERÍA OESTE",new Vector3(-25,4.8f,-25),.10f);
            WorldLabel("04 · GALERÍA ESTE",new Vector3(25,4.8f,-12),.10f);
            Dress();Stars();Supplies();
            arena.pickups=pickups.ToArray();
            arena.heroes=new SurvivalHero[2];
            for(int i=0;i<2;i++)
            {
                var go=new GameObject(i==0?"Jugador 1 · Guerrero":"Jugador 2 · Mago");go.transform.position=new Vector3(i==0?-2.2f:2.2f,0,-9);
                var h=go.AddComponent<SurvivalHero>();h.id=i;h.arena=arena;arena.heroes[i]=h;
                Circle(i==0?"Marca azul":"Marca violeta",go.transform.position+Vector3.up*.035f,.65f,i==0?blue:violet).transform.SetParent(go.transform,true);
            }
            var camera=new GameObject("Cámara compartida · encuadre de ambos").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=48;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.50f,.68f,.78f);camera.gameObject.AddComponent<AudioListener>();
            var follow=camera.gameObject.AddComponent<SurvivalCamera>();follow.arena=arena;arena.sharedCamera=follow;follow.Fit(1,true);
            var light=new GameObject("Sol de la plaza").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.65f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.ambientLight=new Color(.65f,.69f,.74f);RenderSettings.skybox=null;RenderSettings.fog=false;
            var hud=arena.gameObject.AddComponent<SurvivalHUD>();hud.arena=arena;
            new GameObject("Conexión de supervivencia").AddComponent<SurvivalSession>();
            var network=new GameObject("Estado compartido de la arena");var networkObject=network.AddComponent<NetworkObject>();network.AddComponent<SurvivalNetworkWorld>();new NetworkObjectBaker().Bake(network);
            // Fusion ignores scene objects with a zero SortKey. This scene has one replicated world.
            networkObject.SortKey=0x02000001u;EditorUtility.SetDirty(networkObject);
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.43f;settings.agentHeight=1.9f;settings.agentClimb=.35f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.12f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,3,-17),new Vector3(68,16,84)),Vector3.zero,Quaternion.identity);
            if(!data)throw new Exception("Navigation bake failed");data.name="Plaza abierta · terraza y galerías laterales";
            string navPath=Folder+"Art/PlazaNavigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(previous){EditorUtility.CopySerialized(data,previous);Object.DestroyImmediate(data);data=previous;}else AssetDatabase.CreateAsset(data,navPath);
            arena.navigation=data;
            var instance=NavMesh.AddNavMeshData(data);
            foreach(var p in arena.pickups.Where(p=>!p.reserveDrop))
                if(!NavMesh.SamplePosition(p.transform.position,out var hit,.65f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-p.transform.position.y)>.4f)throw new Exception("Pickup off navigation: "+p.name+" "+p.transform.position);
            foreach(var from in new[]{new Vector3(0,0,-10),new Vector3(0,4,16),new Vector3(-25,2.8f,-25),new Vector3(25,2.8f,-12)})
            {
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(from,new Vector3(10,4,17),NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Upper floor path disconnected");
            }
            instance.Remove();
            EditorSceneManager.SaveScene(scene,ScenePath);
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Work+"build-report.txt",$"Scene ready: {ScenePath}\nStars: {pickups.Count(p=>p.kind==SupplyKind.Star)}; pickups: {pickups.Count}\nNavMesh paths ground to upper floor: PASS\n5 waves: 4,6,8,10,12 (last enemy giant); 30s rest; max alive 6.\n");
            Debug.Log("SURVIVAL SCENE BUILT");
        }
        static Material Mat(string name,Color color)
        {string path=Folder+"Art/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_Smoothness",.12f);return mat;}
        static GameObject Box(string name,Vector3 at,Vector3 size,Material mat,bool collision)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(collision?geometry:artRoot);o.transform.position=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;
            if(collision)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(at,Quaternion.identity,Vector3.one),size=size,area=0});else Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }
        static GameObject Circle(string name,Vector3 at,float radius,Material mat)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(artRoot);o.transform.position=at;o.transform.localScale=new Vector3(radius*2,.018f,radius*2);o.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
        static void Stairs(float x)=>StairFlight("Escalera "+(x<0?"oeste":"este"),new Vector3(x,0,5),4,14,4,false,"Rampa"+(x<0?"Oeste":"Este"));
        static void StairFlight(string name,Vector3 center,float width,float length,float rise,bool reverse,string assetName)
        {
            float x=width*.5f,z=length*.5f;
            var mesh=new Mesh();mesh.name=name;
            mesh.vertices=new[]{new Vector3(-x,0,-z),new Vector3(x,0,-z),new Vector3(-x,rise,z),new Vector3(x,rise,z),new Vector3(-x,-.3f,-z),new Vector3(x,-.3f,-z),new Vector3(-x,-.3f,z),new Vector3(x,-.3f,z)};
            mesh.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,4,2,2,4,6,1,3,5,3,7,5,2,6,3,3,6,7,0,1,4,1,5,4};mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"Art/"+assetName+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var ramp=new GameObject(name+" · colisión continua");ramp.transform.SetParent(geometry);ramp.transform.position=center;ramp.transform.rotation=Quaternion.Euler(0,reverse?180:0,0);ramp.AddComponent<MeshCollider>().sharedMesh=mesh;
            ramp.AddComponent<MeshFilter>().sharedMesh=mesh;ramp.AddComponent<MeshRenderer>().sharedMaterial=rock;
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh,transform=ramp.transform.localToWorldMatrix,area=0});
            int steps=Mathf.CeilToInt(rise/.2f);float step=rise/steps,depth=length/steps;
            for(int i=0;i<steps;i++)Box(name+" · peldaño "+(i+1),ramp.transform.TransformPoint(new Vector3(0,(i+.5f)*step,-z+(i+.5f)*depth)),new Vector3(width,step,depth),i%2==0?rock:floor,false);
        }
        static void SideFloors()
        {
            foreach(int side in new[]{-1,1})
            {
                bool west=side<0;string label=west?"Galería oeste":"Galería este";
                float z=west?-25:-12.5f,length=west?18:19,x=side*25;
                Box(label+" · base de mampostería",new Vector3(x,1.4f,z),new Vector3(9,2.8f,length),rock,true);
                Box(label+" · suelo transitable",new Vector3(x,2.825f,z),new Vector3(9,.05f,length),floor,false);
                StairFlight(label+" · acceso sur",new Vector3(x,0,z-length*.5f-4.9f),3.6f,9.8f,2.8f,false,west?"GaleriaOesteSur":"GaleriaEsteSur");
                StairFlight(label+" · acceso norte",new Vector3(x,0,z+length*.5f+4.9f),3.6f,9.8f,2.8f,true,west?"GaleriaOesteNorte":"GaleriaEsteNorte");
                // Low outer parapet; the wide inner edge remains open for jumping back to the plaza.
                Box(label+" · pretil exterior",new Vector3(side*29.3f,3.15f,z),new Vector3(.35f,.7f,length),rock,true);
                for(int i=0;i<4;i++)Box(label+" · refuerzo de pared",new Vector3(side*20.5f,1.25f,z-length*.36f+i*length*.24f),new Vector3(.28f,2.5f,.8f),wood,false);
                PlaceKit("Faroles/Lamp",new Vector3(side*28.3f,2.8f,z+length*.34f),new Vector3(.6f,3,.6f),0);
            }
            foreach(var at in new[]{new Vector3(-27.8f,2.8f,-25.1f),new Vector3(27.5f,2.8f,-9.4f),new Vector3(-10,0,-42),new Vector3(14,0,-27)})
            {
                Box("Grupo de barriles · cobertura",at+Vector3.up*.65f,new Vector3(1.5f,1.3f,1.2f),wood,true).GetComponent<Renderer>().enabled=false;
                PlaceKit("Decoracion/Barel2",at,new Vector3(1.5f,1.3f,1.2f),22);
            }
        }
        static Bounds Bounds(GameObject go){var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
        static GameObject PlaceKit(string key,Vector3 bottom,Vector3 size,float yaw)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Models/MedievalModular/Prefabs/"+key+".prefab");if(!prefab)return null;
            var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,artRoot);o.transform.rotation=Quaternion.Euler(0,yaw,0);var b=Bounds(o);o.transform.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);b=Bounds(o);o.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);foreach(var c in o.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);return o;
        }
        static void Dress()
        {
            PlaceKit("Casas/RH4",new Vector3(-12,4,27),new Vector3(9,9,8),180);
            PlaceKit("Casas/RH2",new Vector3(12,4,27),new Vector3(9,9,8),180);
            PlaceKit("Torres/Tower",new Vector3(-35,0,15),new Vector3(6,12,6),0);
            PlaceKit("Torres/Tower",new Vector3(35,0,15),new Vector3(6,12,6),0);
            foreach(float x in new[]{-35f,35})foreach(float z in new[]{-36f,-12f,4})
            {
                PlaceKit("Terreno/Ground",new Vector3(x,-1.2f,z),new Vector3(7,1.2f,9),0);
                PlaceKit("Terreno/RockA",new Vector3(x,0,z),new Vector3(3,2.4f,2.5f),25);
            }
            foreach(float x in new[]{-13f,13})foreach(float z in new[]{-34f,10})PlaceKit("Faroles/Lamp",new Vector3(x,0,z),new Vector3(.7f,3.6f,.7f),0);
            var grass=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Models/NaturalezaModular/Prefabs/Vegetacion/GrassClump1.prefab");
            if(grass)foreach(float x in new[]{-29.7f,29.7f})for(int i=0;i<6;i++)
            {var o=(GameObject)PrefabUtility.InstantiatePrefab(grass,artRoot);var b=Bounds(o);o.transform.localScale=Vector3.one*(1.1f/b.size.y);b=Bounds(o);o.transform.position+=new Vector3(x,0,-16+i*4)-new Vector3(b.center.x,b.min.y,b.center.z);}
        }
        static void Stars()
        {
            // Short trails with wide gaps, rather than a carpet of collectibles.
            for(int side=-1;side<=1;side+=2)
            {
                foreach(float z in new[]{-15f,-1,8})for(int i=0;i<6;i++)Pickup(SupplyKind.Star,new Vector3(side*((z< -5?10:5)+i*1.35f),0,z),false);
                for(int i=0;i<8;i++){float z=-1+i*1.6f;Pickup(SupplyKind.Star,new Vector3(side*18,(z+2)*4/14,z),false);}
                for(int i=0;i<4;i++)
                {
                    Pickup(SupplyKind.Star,new Vector3(side*22,2.8f,(side<0?-30:-19)+i*1.3f),false);
                    Pickup(SupplyKind.Star,new Vector3(side*(side<0?24:25),2.8f,(side<0?-22.8f:-11.5f)+i*1.3f),false);
                }
            }
            for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20;Pickup(SupplyKind.Star,new Vector3(Mathf.Cos(a)*9.5f,0,Mathf.Sin(a)*9.5f),false);}
            foreach(float x in new[]{-21f,-7,7,21})for(int i=0;i<5;i++)Pickup(SupplyKind.Star,new Vector3(x+(i-2)*.9f,0,-38+Mathf.Sin(i*.7f)*2),false);
        }
        static void Supplies()
        {
            // Small, uneven groups reward moving through the whole arena instead of visiting a shop row.
            Supply(SupplyKind.Armor,new Vector3(-8,0,-14));Supply(SupplyKind.Fire,new Vector3(8,0,-24));
            Supply(SupplyKind.Ice,new Vector3(-14,0,-36));Supply(SupplyKind.Protection,new Vector3(12,0,-4));
            Supply(SupplyKind.Health,new Vector3(-5,0,-19));Supply(SupplyKind.Health,new Vector3(6,0,-45));
            Supply(SupplyKind.Armor,new Vector3(15,0,-40));Supply(SupplyKind.Armor,new Vector3(-13,0,6));
            Supply(SupplyKind.Health,new Vector3(-12,0,-2));Supply(SupplyKind.WarriorLife,new Vector3(-7,0,-48));Supply(SupplyKind.MageLife,new Vector3(13,0,8));
            Supply(SupplyKind.Armor,new Vector3(-25.8f,2.8f,-29.2f));Supply(SupplyKind.Fire,new Vector3(-23,2.8f,-23));
            Supply(SupplyKind.Health,new Vector3(-26.8f,2.8f,-18.5f));Supply(SupplyKind.MageLife,new Vector3(-24.5f,2.8f,-32.2f));
            Supply(SupplyKind.Armor,new Vector3(26.5f,2.8f,-16.5f));Supply(SupplyKind.Ice,new Vector3(23,2.8f,-6.1f));
            Supply(SupplyKind.Protection,new Vector3(26.8f,2.8f,-4.8f));Supply(SupplyKind.Health,new Vector3(23,2.8f,-12.8f));Supply(SupplyKind.WarriorLife,new Vector3(25.7f,2.8f,-19.6f));
            Supply(SupplyKind.Fire,new Vector3(-10,4,17.5f));Supply(SupplyKind.Ice,new Vector3(12.8f,4,14.4f));
            Supply(SupplyKind.Health,new Vector3(4,4,17.2f));Supply(SupplyKind.Protection,new Vector3(-2.5f,4,14.5f));
        }
        static void Supply(SupplyKind kind,Vector3 at)
        {
            Pickup(kind,at,true);
            Circle("Señal de suministro",at+Vector3.up*.035f,.7f,kind==SupplyKind.Health?green:kind==SupplyKind.Armor?blue:gold);
        }
        static SurvivalPickup Pickup(SupplyKind kind,Vector3 at,bool recharge)
        {
            var go=new GameObject(kind==SupplyKind.Star?"Estrella "+pickups.Count:"Suministro · "+kind);go.transform.position=at;
            var p=go.AddComponent<SurvivalPickup>();p.id=pickups.Count;p.kind=kind;p.rechargeEachRest=recharge;pickups.Add(p);
            var holder=new GameObject("Modelo");holder.transform.SetParent(go.transform,false);holder.transform.localPosition=Vector3.up*.7f;
            string key=kind==SupplyKind.Star?"LevelModels/Star":kind==SupplyKind.Fire?"VillageProps/LibroFuego":kind==SupplyKind.Ice?"VillageProps/LibroHielo":kind==SupplyKind.Protection?"VillageProps/LibroProteccion":kind==SupplyKind.WarriorLife?"VillageProps/VidaExtra":kind==SupplyKind.MageLife?"VillageProps/VidaMago":"VillageProps/Cofre";
            if(kind==SupplyKind.Health)key="VillageProps/PocionSalud";
            var prefab=Resources.Load<GameObject>(key);var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,holder.transform);model.transform.localRotation=kind==SupplyKind.Health?Quaternion.identity:Quaternion.Euler(kind==SupplyKind.Star||kind==SupplyKind.Fire||kind==SupplyKind.Ice||kind==SupplyKind.Protection?0:-90,180,0);
            var b=Bounds(model);float target=kind==SupplyKind.Star?.48f:kind==SupplyKind.Armor?1.15f:.85f;model.transform.localScale*=target/Mathf.Max(b.size.x,b.size.y,b.size.z);b=Bounds(model);holder.transform.position+=at+Vector3.up*.7f-b.center;p.art=holder.transform;
            if(kind!=SupplyKind.Star)WorldLabel(kind==SupplyKind.Armor?"ARMADURA":kind==SupplyKind.Health?"SALUD +35":kind==SupplyKind.WarriorLife?"VIDA +1 G":kind==SupplyKind.MageLife?"VIDA +1 M":kind==SupplyKind.Fire?"FUEGO":kind==SupplyKind.Ice?"HIELO":"PROTECCIÓN",at+Vector3.up*1.6f,.095f).transform.SetParent(go.transform,true);
            return p;
        }
        static GameObject WorldLabel(string text,Vector3 position,float size)
        {var o=new GameObject(text);o.transform.SetParent(artRoot);o.transform.position=position;o.transform.rotation=Quaternion.Euler(35,0,0);var t=o.AddComponent<TextMesh>();t.text=text;t.fontSize=48;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(1,.91f,.65f);return o;}
    }
}

