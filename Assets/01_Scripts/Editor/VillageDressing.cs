using UnityEngine;
using UnityEditor;
public static class VillageDressing
{
    static Transform root;
    static Material wood,green,lightGreen,white,stone,red,gold,water,distant;
    static Material M(string name, Color color)
    {
        string path="Assets/03_Materials/Village_"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.color=color;material.SetFloat("_Smoothness",.08f);return material;
    }
    static GameObject S(string name,PrimitiveType shape,Vector3 position,Vector3 size,Material material)
    {
        var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(root);o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static void Cube(string name,float x,float y,float z,float w,float h,float d,Material m){S(name,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(w,h,d),m);}
    static void Fence(float x,float length)
    {
        for(float p=x;p<x+length;p+=1.4f)Cube("Poste cerca",p,.65f,2,.14f,1.3f,.14f,wood);
        Cube("Travesano cerca",x+length/2,.85f,2,length,.12f,.12f,wood);
        Cube("Travesano cerca",x+length/2,.4f,2,length,.12f,.12f,wood);
    }
    public static void Add()
    {
        wood=M("DarkTimber",new Color(.24f,.16f,.12f));green=M("Bush",new Color(.21f,.4f,.16f));lightGreen=M("Meadow",new Color(.54f,.68f,.25f));white=M("Cloud",new Color(.94f,.94f,.87f));stone=M("Rock",new Color(.43f,.49f,.49f));red=M("Berry",new Color(.74f,.24f,.16f));gold=M("Flower",new Color(.98f,.76f,.22f));water=M("ToxicFoam",new Color(.75f,1,.25f));distant=M("DistantHills",new Color(.38f,.6f,.66f));
        for(int i=0;i<8;i++)
        {
            float x=i*24;
            root=new GameObject("Decoracion sector "+(i+1).ToString("00")).transform;
            if(i!=3)Cube("Pradera de fondo",x+11,-.6f,7,24,1,10,lightGreen);
            for(int j=0;j<3;j++)S("Nube",PrimitiveType.Sphere,new Vector3(x+j*2,10.8f+(i%2),28),new Vector3(4,1.5f,1),white);
            S("Montana distante",PrimitiveType.Sphere,new Vector3(x+12,3,27),new Vector3(28,17,3),distant);
            for(int j=0;j<5;j++)
            {
                float px=x+2+j*4.4f;
                if(i==3&&px>x+6&&px<x+18)continue;
                S("Arbusto",PrimitiveType.Sphere,new Vector3(px,.6f,3),new Vector3(1.8f,1.3f,1),j%2==0?green:lightGreen);
                Cube("Tallo flor",px+.7f,.35f,-1.1f,.05f,.7f,.05f,green);
                S("Flor",PrimitiveType.Sphere,new Vector3(px+.7f,.7f,-1.1f),Vector3.one*.2f,j%2==0?gold:red);
                S("Piedra",PrimitiveType.Sphere,new Vector3(px-.7f,.18f,-.9f),new Vector3(.65f,.35f,.45f),stone);
            }
            // Legs make the simple sheep readable even at platform-camera distance.
            for(int j=0;j<4;j++)Cube("Pata oveja",x+8+(j<2?-.4f:.4f),.23f,4+(j%2==0?-.2f:.2f),.12f,.5f,.12f,wood);
            if(i==0||i==1||i==4)Fence(x+2,7);
            if(i==1)
            {
                for(int j=0;j<5;j++){Cube("Surco huerto",x+13+j*1.3f,.15f,3.5f,.8f,.3f,1.5f,wood);S("Hortaliza",PrimitiveType.Sphere,new Vector3(x+13+j*1.3f,.5f,3.5f),Vector3.one*.6f,lightGreen);}
                Cube("Carreta",x+20,.8f,4,2.2f,.6f,1.3f,wood);
                for(int j=0;j<2;j++){var wheel=S("Rueda",PrimitiveType.Cylinder,new Vector3(x+19.4f+j*1.2f,.4f,3.3f),new Vector3(.8f,.12f,.8f),stone);wheel.transform.rotation=Quaternion.Euler(90,0,0);}
            }
            if(i==3)for(int j=0;j<9;j++){var bubble=S("Burbuja venenosa",PrimitiveType.Sphere,new Vector3(x+8+j,-1.45f,-.5f),Vector3.one*(.15f+j%3*.09f),water);bubble.AddComponent<VillageAmbient>().bob=.15f;}
            if(i==4){for(int j=0;j<2;j++){Cube("Puesto de mercado",x+10+j*4,1,4,2.8f,.35f,1.5f,wood);Cube("Toldo",x+10+j*4,3,4,3,.2f,2,j==0?red:gold);Cube("Soporte toldo",x+9+j*4,1.5f,4,.12f,3,.12f,wood);}}
            if(i==6){for(int j=0;j<5;j++){float px=x+3+j*3;Cube("Tronco bosque",px,2.5f,6,.45f,5,.45f,wood);S("Follaje bosque",PrimitiveType.Sphere,new Vector3(px,6+j%2,6),new Vector3(3.5f,4,2),green);}Fence(x+18,5);}
            if(i==7){Cube("Bandera salida",x+18,4,2,.12f,4,.12f,wood);Cube("Estandarte",x+18.8f,5.4f,2,1.5f,1,.12f,gold);}
        }
        // Rotate existing mill blades around a shared hub.
        root=new GameObject("Rotor del molino").transform;root.position=new Vector3(135,5,6);
        foreach(var t in Object.FindObjectsByType<Transform>())if(t.name=="Aspa del molino")t.SetParent(root,true);
        root.gameObject.AddComponent<VillageAmbient>().rotation=new Vector3(0,0,18);
        // Visible armor stays a single logical set.
        var hero=Object.FindAnyObjectByType<VillageGame>().warrior;
        root=new GameObject("Armadura completa visual").transform;root.SetParent(hero.transform,false);hero.armorVisual=root.gameObject;
        for(int j=0;j<2;j++)
        {
            var shoulder=S("Hombrera",PrimitiveType.Sphere,hero.transform.position+new Vector3(j==0?-.42f:.42f,.55f,0),new Vector3(.4f,.28f,.65f),stone);shoulder.transform.SetParent(root,true);
            var knee=S("Rodillera",PrimitiveType.Sphere,hero.transform.position+new Vector3(j==0?-.2f:.2f,-.55f,-.4f),new Vector3(.25f,.3f,.15f),stone);knee.transform.SetParent(root,true);
        }
    }
}

