using UnityEngine;
public static class VillageTerrainProfile
{
    public static readonly Vector2[][] Sections={
        new[]{new Vector2(23,0),new Vector2(26,0),new Vector2(30,1.4f),new Vector2(32,1.4f),new Vector2(37,-.8f),new Vector2(40,-.8f),new Vector2(47,0)},
        new[]{new Vector2(47,0),new Vector2(54,-.8f),new Vector2(62,-1.2f),new Vector2(70,0),new Vector2(71,0)},
        new[]{new Vector2(143,0),new Vector2(147,.8f),new Vector2(152,2.2f),new Vector2(155,2.2f),new Vector2(160,-.8f),new Vector2(164,-.8f),new Vector2(167,0)},
        new[]{new Vector2(167,0),new Vector2(171,0),new Vector2(176,1.5f),new Vector2(179,1.5f),new Vector2(185,0),new Vector2(191,0)}
    };
    public static float Height(float x)
    {
        foreach(var points in Sections)for(int i=0;i<points.Length-1;i++)if(x>=points[i].x&&x<=points[i+1].x)
            return Mathf.Lerp(points[i].y,points[i+1].y,Mathf.InverseLerp(points[i].x,points[i+1].x,x));
        return 0;
    }
}
