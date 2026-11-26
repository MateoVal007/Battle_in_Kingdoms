using UnityEngine;

public static class VillagePropModels
{
    public static Transform Attach(GameObject owner, string kind)
    {
        string key = kind == "Cofre" ? "Cofre" : kind == "Fuego" ? "LibroFuego" : kind == "Hielo" ? "LibroHielo" : kind == "Protección" ? "LibroProteccion" : kind == "Vida extra" ? "VidaExtra" : kind == "Vida extra mago" ? "VidaMago" : null;
        if (kind == "Salud") key = "PocionSalud";
        if (key == null) return null;
        string name = "Modelo · " + key;
        var existing = owner.transform.Find(name);
        if (existing) return existing;
        var prefab = Resources.Load<GameObject>("VillageProps/" + key);
        if (!prefab) return null;
        foreach (var renderer in owner.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        var holder = new GameObject(name);
        holder.transform.SetParent(owner.transform,false);
        var parentScale = owner.transform.lossyScale;
        holder.transform.localScale = new Vector3(1/parentScale.x,1/parentScale.y,1/parentScale.z);
        var model = Object.Instantiate(prefab,holder.transform,false);
        model.transform.localRotation = key == "PocionSalud" ? Quaternion.identity : Quaternion.Euler(key.StartsWith("Libro")?0:-90,180,0);
        var bounds = Bounds(model);
        bool chest = kind == "Cofre";
        var collider = owner.GetComponent<Collider>();
        var target = chest && collider ? collider.bounds : new Bounds(owner.transform.position,new Vector3(.8f,.7f,.7f));
        var size = target.size;
        float factor = Mathf.Min(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z)*.98f;
        model.transform.localScale *= factor;
        bounds = Bounds(model);
        holder.transform.position += chest
            ? new Vector3(target.center.x-bounds.center.x,target.min.y-bounds.min.y,target.center.z-bounds.center.z)
            : target.center-bounds.center;
        return holder.transform;
    }

    static Bounds Bounds(GameObject item)
    {
        var renderers = item.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}

