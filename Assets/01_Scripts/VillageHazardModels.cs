using UnityEngine;

public static class VillageHazardModels
{
    public static Transform Attach(VillageZone zone)
    {
        if(zone.kind!="Espinos" && zone.kind!="Veneno") return null;
        var owner=zone.gameObject;
        string name="Modelo · "+zone.kind;
        var existing=owner.transform.Find(name);
        if(existing) return existing;
        var prefab=Resources.Load<GameObject>("VillageHazards/"+zone.kind);
        var collider=owner.GetComponent<Collider>();
        if(!prefab||!collider) return null;
        // Keep the authored damage trigger and all ambient poison movement intact.
        var old=owner.GetComponent<Renderer>();
        if(old) old.enabled=false;
        if(zone.kind=="Espinos") foreach(var r in owner.GetComponentsInChildren<Renderer>()) if(r.name=="Espina")r.enabled=false;
        var holder=new GameObject(name).transform;
        holder.SetParent(owner.transform,false);
        var scale=owner.transform.lossyScale;
        holder.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        var target=collider.bounds;
        if(zone.kind=="Espinos" && collider is BoxCollider box)
            target=new Bounds(owner.transform.TransformPoint(box.center),Vector3.Scale(box.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))));
        if(zone.kind=="Veneno" && owner.TryGetComponent<MeshFilter>(out var originalMesh) && old)
        {
            var fill=new GameObject("Fondo continuo del veneno");
            fill.transform.SetParent(holder,false);
            fill.AddComponent<MeshFilter>().sharedMesh=originalMesh.sharedMesh;
            fill.AddComponent<MeshRenderer>().sharedMaterial=old.sharedMaterial;
            fill.transform.localScale=new Vector3(target.size.x,.05f,target.size.z);
            fill.transform.position=new Vector3(target.center.x,target.max.y-.215f,target.center.z);
        }
        int count=Mathf.Max(1,Mathf.CeilToInt(target.size.x/(zone.kind=="Espinos"?.6f:2f)));
        float width=target.size.x/count;
        for(int i=0;i<count;i++)
        {
            var model=Object.Instantiate(prefab,holder,false);
            model.transform.localRotation=Quaternion.Euler(-90,180,0);
            var b=Bounds(model);
            float height=zone.kind=="Veneno"?Mathf.Min(.22f,target.size.y):target.size.y;
            float x=target.min.x+(i+.5f)*width;
            float left=VillageTerrainProfile.Height(x-width*.5f),right=VillageTerrainProfile.Height(x+width*.5f);
            float angle=zone.kind=="Espinos"?Mathf.Atan2(right-left,width)*Mathf.Rad2Deg:0;
            float length=zone.kind=="Espinos"?Mathf.Sqrt(width*width+(right-left)*(right-left)):width+.015f;
            model.transform.localScale=Vector3.Scale(model.transform.localScale,new Vector3(length/b.size.x,height/b.size.y,target.size.z/b.size.z));
            b=Bounds(model);
            if(zone.kind=="Espinos")
            {
                var ground=new Vector3(x,VillageTerrainProfile.Height(x)+.13f,target.center.z);
                model.transform.position+=ground-new Vector3(b.center.x,b.min.y,b.center.z);
                model.transform.RotateAround(ground,Vector3.forward,angle);
            }
            else model.transform.position+=new Vector3(x-b.center.x,target.max.y-b.max.y,target.center.z-b.center.z);

        }
        return holder;
    }
    static Bounds Bounds(GameObject o)
    {
        var renderers=o.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
