using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
public static class VillageBuilder {
 static Material grass,soil,wood,stone,leaf,poison,red,blue,purple,gold,cream,roof;
 static Transform sector;
 static Material Mat(string n,Color c){string path="Assets/03_Materials/Village_"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Smoothness",.15f);return m;}
 static GameObject Shape(string n,PrimitiveType t,Vector3 p,Vector3 s,Material m,bool solid=true){var o=GameObject.CreatePrimitive(t);o.name=n;o.transform.SetParent(sector);o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;if(!solid)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
 static GameObject Box(string n,float x,float y,float w,float h,Material m,float z=0,float depth=2.5f,bool solid=true){return Shape(n,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(w,h,depth),m,solid);}
 static void Label(string text,float x,float y,float size=.35f){var o=new GameObject(text);o.transform.SetParent(sector);o.transform.position=new Vector3(x,y,-1.8f);var t=o.AddComponent<TextMesh>();t.text=text;t.characterSize=size*.2f;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.12f,.18f,.23f);}
 static void Platform(float x,float y,float width){Box("Plataforma de madera",x,y,width,.45f,wood).AddComponent<VillageOneWayPlatform>();Box("Borde claro",x,y+.24f,width,.08f,gold,0,2.5f,false);}
 static void Ground(float x,float width){Box("Terreno",x,-1.5f,width,3,soil);Box("Hierba",x,.06f,width,.12f,grass);}
 static void Zone(string type,float x,float y,float width=1){var o=Box(type,x,y,width,type=="Checkpoint"?3:.5f,type=="Veneno"?poison:type=="Espinos"?red:type=="Armadura"?blue:type=="Fuego"?red:type=="Hielo"?blue:gold,0,2);o.GetComponent<Collider>().isTrigger=true;var z=o.AddComponent<VillageZone>();z.kind=type;z.checkpointX=x;Label(type,x,y+1.2f,.23f);
 if(type=="Espinos"){for(int k=0;k<Mathf.CeilToInt(width*2);k++){var spike=Shape("Espina",PrimitiveType.Cube,new Vector3(x-width/2+k*.5f+.25f,y+.25f,-.3f),new Vector3(.5f,.5f,.7f),stone,false);spike.transform.rotation=Quaternion.Euler(0,0,45);spike.transform.SetParent(o.transform,true);}}
 if(type=="Fuego"||type=="Hielo"||type=="Protección"){o.transform.localScale=new Vector3(.8f,.4f,.65f);var pages=Box("Paginas del libro",x,y+.23f,.7f,.1f,cream,0,.55f,false);pages.transform.SetParent(o.transform,true);}}
 static void Tree(float x,float y,float z,float scale=1){Shape("Tronco",PrimitiveType.Cylinder,new Vector3(x,y+2*scale,z),new Vector3(.55f*scale,2*scale,.55f*scale),wood,false);Shape("Copa",PrimitiveType.Sphere,new Vector3(x,y+5*scale,z),new Vector3(4,5,3)*scale,leaf,false);}
 static void House(float x,float y,float width=6){Box("Casa / entramado",x,y+2,width,4,cream,7,3,false);Box("Viga central",x,y+2,.18f,4,wood,5.4f,.2f,false);Box("Viga horizontal",x,y+3,width,.18f,wood,5.4f,.2f,false);Box("Puerta",x,y+1,1.2f,2,wood,5.3f,.2f,false);Box("Ventana",x-1.8f,y+2.5f,1,1,blue,5.3f,.2f,false);
 var a=Box("Tejado izquierdo",x-width/4,y+4.8f,width*.6f,.35f,roof,7,4,false);a.transform.rotation=Quaternion.Euler(0,0,30);var b=Box("Tejado derecho",x+width/4,y+4.8f,width*.6f,.35f,roof,7,4,false);b.transform.rotation=Quaternion.Euler(0,0,-30);}
 static VillageHero Hero(bool mage,float x){var o=Shape(mage?"Mago":"Guerrero",PrimitiveType.Capsule,new Vector3(x,1.3f,0),Vector3.one,mage?purple:blue,false);var cc=o.AddComponent<CharacterController>();cc.height=2;cc.radius=.42f;cc.stepOffset=.3f;var h=o.AddComponent<VillageHero>();h.isMage=mage;h.armor=0;
 var head=Shape("Cabeza",PrimitiveType.Sphere,new Vector3(x,2.1f,0),Vector3.one*.65f,cream,false);head.transform.SetParent(o.transform,true);
 if(mage){var staff=Box("Baston",x+.6f,1.5f,.12f,2,wood,-.2f,.12f,false);staff.transform.SetParent(o.transform,true);var crystal=Shape("Cristal baston",PrimitiveType.Sphere,new Vector3(x+.6f,2.6f,-.2f),Vector3.one*.35f,poison,false);crystal.transform.SetParent(o.transform,true);}else{
 var sword=Box("Espada",x+.7f,1.5f,.15f,1.6f,stone,-.2f,.15f,false);sword.transform.SetParent(o.transform,true);h.sword=sword.transform;
 var shield=Shape("Escudo",PrimitiveType.Sphere,new Vector3(x+.6f,1.3f,-.4f),new Vector3(.65f,1,.2f),gold,false);shield.transform.SetParent(o.transform,true);h.shield=shield.transform;
 var arc=new GameObject("Arco de espada");arc.transform.SetParent(o.transform,false);var line=arc.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=14;line.widthMultiplier=.18f;line.sharedMaterial=Mat("Slash",new Color(.3f,1,1));for(int i=0;i<14;i++){float a=Mathf.Lerp(-1.1f,1.1f,i/13f);line.SetPosition(i,new Vector3(Mathf.Cos(a)*1.5f,Mathf.Sin(a)*1.5f,0));}h.arc=arc.transform;arc.SetActive(false);
 }return h;}
 static void Chest(float x,float y,string reward){
 y+=.2f;var o=Box("Cofre · "+reward,x,y,1.5f,1.4f,wood,0,1);var chest=o.AddComponent<VillageChest>();chest.reward=reward;
 var hinge=new GameObject("Bisagra tapa").transform;hinge.SetParent(o.transform);hinge.position=new Vector3(x,y+.7f,.5f);hinge.localScale=new Vector3(1/o.transform.localScale.x,1/o.transform.localScale.y,1/o.transform.localScale.z);chest.lid=hinge;
 var lid=Box("Tapa",x,y+.825f,1.6f,.25f,wood,0,1.1f,false);lid.transform.SetParent(hinge,true);
 for(int k=0;k<2;k++){var band=Box("Herraje",x+(k==0?-.48f:.48f),y,.12f,1.45f,gold,-.53f,.08f,false);band.transform.SetParent(o.transform,true);}
 var lockObj=Box("Cerradura",x,y+.2f,.22f,.28f,gold,-.57f,.1f,false);lockObj.transform.SetParent(o.transform,true);
 Label("COFRE · golpea / dispara",x,y+1.7f,.2f);
 }
 static void Enemy(float x,float y,bool ranged=false){var o=Shape(ranged?"Bandido tirador":"Bandido patrulla",PrimitiveType.Capsule,new Vector3(x,y,0),new Vector3(.85f,1,.85f),red);o.AddComponent<VillageEnemy>().ranged=ranged;}
 [MenuItem("Battle in Kingdoms/Generar nivel Pueblo del Roble")]
 public static void Build(){
 if(EditorApplication.isPlaying)return;
 if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 grass=Mat("Grass",new Color(.39f,.63f,.22f));soil=Mat("Earth",new Color(.4f,.28f,.17f));wood=Mat("Wood",new Color(.35f,.2f,.12f));stone=Mat("Stone",new Color(.55f,.61f,.65f));leaf=Mat("Leaves",new Color(.18f,.42f,.29f));poison=Mat("Poison",new Color(.59f,.88f,.12f));red=Mat("Danger",new Color(.8f,.25f,.18f));blue=Mat("Blue",new Color(.15f,.44f,.75f));purple=Mat("Mage",new Color(.5f,.24f,.67f));gold=Mat("Gold",new Color(.95f,.72f,.28f));cream=Mat("Plaster",new Color(.9f,.82f,.62f));roof=Mat("Roof",new Color(.48f,.25f,.19f));
 string[] titles={"01 · LLEGADA","02 · HUERTOS","03 · TEJADOS","04 · ARROYO","05 · PLAZA","06 · MOLINO","07 · BOSQUE","08 · PUERTA"};
 for(int i=0;i<8;i++){float x=i*24;sector=new GameObject(titles[i]).transform;Label(titles[i],x+10,10,.48f);
 if(i==3){Ground(x+3,8);Ground(x+21,8);Box("Lecho del arroyo",x+12,-2.5f,10,1,soil);Zone("Veneno",x+12,-1.7f,10);}else Ground(x+11,24);
 House(x+5,0,i==4?9:6);if(i!=3)House(x+18,0);Tree(x+1,0,12,1.3f);Tree(x+20,0,14,1.6f);
 Shape("Colina lejana",PrimitiveType.Sphere,new Vector3(x+8,0,24),new Vector3(28,18,6),leaf,false);
 Shape("Oveja / cuerpo",PrimitiveType.Sphere,new Vector3(x+8,.7f,4),new Vector3(1.3f,.8f,.7f),cream,false);Shape("Oveja / cabeza",PrimitiveType.Sphere,new Vector3(x+8.65f,.8f,4),Vector3.one*.4f,stone,false);
 if(i==0){Platform(10,1,3);Platform(15,2,3);Label("A/D + W     |     Flechas + Arriba",8,4,.25f);Zone("Checkpoint",2,1);Zone("Fuego",18,1);Chest(6,.65f,"Equipo");Chest(20,.65f,"Salud");}
 if(i==1){Platform(x+3,1,3);Platform(x+7,2.5f,3);Platform(x+12,4,6);Platform(x+19,2.5f,3);Zone("Espinos",x+12,.4f,4);Zone("Armadura",x+12,4.8f);Enemy(x+21,1.1f);}
 if(i==2){Platform(x+2,1.3f,3);Platform(x+6,2.8f,3);Platform(x+11,4.2f,5);Platform(x+18,4.2f,5);Platform(x+22,2,3);Zone("Hielo",x+16,5);Chest(x+18,5,"Hielo");Enemy(x+13,1.1f);Label("ARRIBA: libros  /  ABAJO: combate",x+12,7,.25f);}
 if(i==3){Platform(x+6,1,3);Platform(x+11,2,6);Platform(x+16,1,3);Platform(x+10,5,4);Platform(x+16,5,3);Platform(x,2,2);Platform(x+3,4,3);Zone("Salud",x+16,5.8f);Platform(x+8,-.8f,1.8f);Platform(x+15,-.8f,1.8f);Chest(x+22,.65f,"Salud");Label("VENENO: salta por las piedras",x+12,-3,.22f);}
 if(i==4){Zone("Checkpoint",x+3,1);Zone("Salud",x+7,1);Zone("Protección",x+13,1);Chest(x+12,.65f,"Salud");Box("Fuente",x+17,1,3,2,stone,4,3,false);Label("REUNIÓN / preparar cooperación",x+12,7,.25f);}
 if(i==5){
 Box("Torre del molino",x+15,3,3,6,stone,9,3,false);
 for(int arm=0;arm<4;arm++){float angle=arm*90+25;var blade=Box("Aspa del molino",x+15,5,7,.35f,wood,6,.3f,false);blade.transform.rotation=Quaternion.Euler(0,0,angle);}
 Platform(x+3,1,3);Platform(x+7,2,3);Zone("Armadura",x+4,1.8f);Box("Sello escudo",132,.2f,3,.25f,blue);Box("Sello magia",137,.2f,3,.25f,purple);Shape("Cristal del molino",PrimitiveType.Sphere,new Vector3(137,4,1),Vector3.one,poison,false);Label("Guerrero en AZUL: G\nMago en MORADO: L\nMantener juntos 3 segundos",134,7,.27f);}
 if(i==6){Platform(x+3,1.4f,3);Platform(x+7,2.8f,3);Platform(x+12,4.2f,5);Platform(x+19,3,4);Zone("Espinos",x+11,.4f,4);Zone("Fuego",x+12,5);Chest(x+18,.65f,"Salud");Enemy(x+20,1.1f,true);Tree(x+10,0,6);}
 if(i==7){Enemy(x+8,1.1f);Enemy(x+13,1.1f,true);Platform(x+5,1.5f,3);Platform(x+10,3,4);Zone("Salud",x+10,3.8f);Box("Arco salida izquierda",185,3,1,6,stone,3);Box("Arco salida derecha",191,3,1,6,stone,3);Box("Dintel salida",188,6,7,1,stone,3);Label("SALIDA / ambos jugadores",187,8,.28f);}
 }
 sector=new GameObject("Sistemas y jugadores").transform;
 var game=new GameObject("Partida local / transporte pendiente").AddComponent<VillageGame>();game.warrior=Hero(false,1);game.mage=Hero(true,3);game.gate=Box("Puerta cooperativa",142,5,1,10,stone).transform;
 Box("Limite izquierdo",-2,4,1,12,stone);Box("Limite derecho",194,4,1,12,stone);
 var camera=new GameObject("Camara lateral").AddComponent<Camera>();camera.transform.position=new Vector3(8,5.8f,-22);camera.transform.rotation=Quaternion.Euler(6,0,0);camera.orthographic=false;camera.fieldOfView=34;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.55f,.76f,.88f);camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();game.view=camera;
 var sun=new GameObject("Sol").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.7f;sun.transform.rotation=Quaternion.Euler(35,-25,0);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.72f,.77f,.85f);
 VillageDressing.Add();
 VillageDesignRevision.ApplyScene();VillageDesignRevision.ApplySolidLayout();VillageEnemyRevision.ApplyScene();VillageLayoutV5.Apply();VillageRevisionV6.Apply();VillageRevisionV7.Apply();
 EditorSceneManager.SaveScene(scene,"Assets/00_Scenes/Pueblo_01_Blockout.unity");AssetDatabase.SaveAssets();
 Directory.CreateDirectory("Docs/Verification");File.WriteAllText("Docs/Verification/village-build.txt","Scene generated in Unity "+Application.unityVersion+"\n"+System.DateTime.Now+"\nObjects: "+Object.FindObjectsByType<Transform>().Length);
 Debug.Log("VILLAGE_BUILD_OK");
 }
}








