using UnityEngine;
public class VillageChest : MonoBehaviour
{
    public string reward="Salud";
    public Transform lid;
    public bool opened;
    public bool keepOnRetry;
    GameObject loot;
    Transform model;
    void Awake(){ model=VillagePropModels.Attach(gameObject,"Cofre"); UpdateModel(); }
    void UpdateModel(){if(!model)return;var tint=new MaterialPropertyBlock();if(opened)tint.SetColor("_BaseColor",new Color(.6f,.6f,.6f));foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(opened?tint:null);}

    public bool LootActive => loot && loot.activeSelf;
    public void ApplyRemote(bool open,bool rewardActive){if(open&&!opened)Hit();if(!open&&opened)ResetChest();if(loot)loot.SetActive(rewardActive);}
    void Update(){if(lid)lid.localRotation=Quaternion.Slerp(lid.localRotation,Quaternion.Euler(opened?-105:0,0,0),1-Mathf.Exp(-8*Time.deltaTime));}
    public void Hit()
    {
        if(opened)return;
        opened=true;UpdateModel();GetComponent<Collider>().enabled=false;
        loot=GameObject.CreatePrimitive(reward=="Salud"?PrimitiveType.Sphere:PrimitiveType.Cube);
        loot.name="Recompensa cofre · "+reward;
        loot.transform.position=transform.position+Vector3.up*1.1f;
        loot.transform.localScale=Vector3.one*.65f;
        loot.GetComponent<Collider>().isTrigger=true;
        if(!VillagePropModels.Attach(loot,reward))loot.GetComponent<Renderer>().material.color=reward=="Vida extra"?new Color(.35f,1,.7f):reward=="Salud"?new Color(1,.25f,.45f):reward=="Hielo"?Color.cyan:reward=="Fuego"?new Color(1,.45f,0):Color.yellow;
        var zone=loot.AddComponent<VillageZone>();zone.kind=reward;zone.spawnedByChest=true;
        var ambient=loot.AddComponent<VillageAmbient>();ambient.bob=.12f;ambient.rotation=new Vector3(0,50,0);
    }
    public void ResetChest(){if(loot){loot.SetActive(false);Destroy(loot);}opened=false;UpdateModel();GetComponent<Collider>().enabled=true;}
}

