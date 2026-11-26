using UnityEngine;

public static class VillageMageHUD
{
    static readonly Texture2D[] wedges=new Texture2D[65];
    static readonly Color[] colors={new Color(1,.43f,.14f),new Color(.25f,.85f,1),new Color(.65f,.5f,1)};
    static Texture2D Disc(int step)
    {
        step=Mathf.Clamp(step,0,64);if(wedges[step])return wedges[step];
        var t=new Texture2D(64,64,TextureFormat.RGBA32,false);t.name="Carga circular "+step;t.hideFlags=HideFlags.HideAndDontSave;t.filterMode=FilterMode.Bilinear;
        var pixels=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float dx=x-31.5f,dy=y-31.5f;float radius=Mathf.Sqrt(dx*dx+dy*dy);
            float angle=Mathf.Repeat(Mathf.Atan2(dx,dy)/(2*Mathf.PI),1);
            pixels[y*64+x]=new Color(1,1,1,angle<=step/64f&&step>0?Mathf.Clamp01(31.5f-radius):0);
        }
        t.SetPixels(pixels);t.Apply();wedges[step]=t;return t;
    }
    static void Circle(Rect r,int amount,Color c){GUI.color=c;GUI.DrawTexture(r,Disc(amount));GUI.color=Color.white;}
    public static void Draw(VillageHero mage)
    {
        GUI.Box(new Rect(954,122,310,145),"");
        var centered=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=13};
        for(int i=0;i<3;i++)
        {
            float x=967+i*99;bool selected=mage.selectedItem==i;
            Circle(new Rect(x+13,130,62,62),64,selected?Color.white:new Color(.25f,.29f,.35f));
            Circle(new Rect(x+16,133,56,56),64,new Color(.08f,.12f,.18f));
            Circle(new Rect(x+18,135,52,52),Mathf.CeilToInt(mage.ItemCharge(i)/VillageHero.ItemMaximum(i)*64),colors[i]);
            GUI.Label(new Rect(x,144,88,28),i==0?"F":i==1?"H":"P",new GUIStyle(centered){fontSize=22,fontStyle=FontStyle.Bold,normal={textColor=Color.white}});
            GUI.Label(new Rect(x,192,88,22),VillageHero.ItemNames[i],centered);
            GUI.Label(new Rect(x,212,88,20),mage.ItemCharge(i)>0?Mathf.CeilToInt(mage.ItemCharge(i))+(i==2?" s":" usos"):"Vacío",centered);
        }
        GUI.Label(new Rect(962,238,294,25),"Q/E elegir · R "+(mage.selectedItem==2?(mage.itemActive?"desactivar protección":"activar protección"):"lanzar (1 carga)"),centered);
    }
}
