using UnityEngine;

// Shared presentation and flight rules; only the existing authoritative projectile simulates damage.
public sealed class EnemyBombVisual : MonoBehaviour
{
    public const float Gravity=16f, Fuse=1.5f, BlastRadius=1.9f, BlastDuration=.7f;
    Renderer core;
    GameObject wick;
    LineRenderer blast;
    Renderer burst;
    Transform[] embers;
    Light flash;
    MaterialPropertyBlock burstTint;
    MaterialPropertyBlock tint;
    static Material dark,flame;
    public static Vector3 ThrowVelocity(Vector3 origin,Vector3 destination)
    {const float flight=.9f;return (destination-origin)/flight+Vector3.up*(Gravity*flight*.5f);}
    public static void Advance(Transform body,ref Vector3 velocity,float dt)
    {
        velocity+=Vector3.down*(Gravity*dt);
        var step=velocity*dt;
        var hits=Physics.SphereCastAll(body.position,.16f,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            var c=hit.collider;
            if(c.GetComponentInParent<VillageHero>()||c.GetComponentInParent<VillageEnemy>()||c.GetComponentInParent<Kingdoms.Survival.SurvivalHero>()||c.GetComponentInParent<Kingdoms.Survival.SurvivalEnemy>())continue;
            body.position=hit.point+hit.normal*.18f;
            velocity=Vector3.Reflect(velocity,hit.normal)*.25f;
            if(hit.normal.y>.55f&&Mathf.Abs(velocity.y)<1)velocity=Vector3.zero;
            return;
        }
        body.position+=step;
    }
    void Awake()
    {
        if(!dark)
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");
            dark=new Material(shader){name="Bomba oscura",color=new Color(.055f,.065f,.07f)};
            flame=new Material(shader){name="Mecha y explosión",color=new Color(1,.42f,.035f)};
        }
        core=GetComponent<Renderer>();core.sharedMaterial=dark;tint=new MaterialPropertyBlock();
        wick=GameObject.CreatePrimitive(PrimitiveType.Sphere);wick.name="Mecha encendida";
        var c=wick.GetComponent<Collider>();c.enabled=false;Destroy(c);
        wick.transform.SetParent(transform,false);wick.transform.localPosition=Vector3.up*.58f;wick.transform.localScale=Vector3.one*.2f;wick.GetComponent<Renderer>().sharedMaterial=flame;
        var ring=new GameObject("Onda de explosión");ring.transform.SetParent(transform,false);blast=ring.AddComponent<LineRenderer>();
        blast.useWorldSpace=true;blast.loop=true;blast.positionCount=48;blast.sharedMaterial=flame;blast.enabled=false;
        var cloud=GameObject.CreatePrimitive(PrimitiveType.Quad);cloud.name="Fuego y humo de explosión";
        c=cloud.GetComponent<Collider>();c.enabled=false;Destroy(c);cloud.transform.SetParent(transform,false);
        burst=cloud.GetComponent<Renderer>();var effect=Resources.Load<Material>("VFX/BombExplosion");burst.sharedMaterial=effect?effect:flame;burst.enabled=false;burstTint=new MaterialPropertyBlock();
        embers=new Transform[8];
        for(int i=0;i<embers.Length;i++)
        {
            var spark=GameObject.CreatePrimitive(PrimitiveType.Sphere);spark.name="Brasa";c=spark.GetComponent<Collider>();c.enabled=false;Destroy(c);
            spark.transform.SetParent(transform,false);spark.transform.localScale=Vector3.one*.25f;spark.GetComponent<Renderer>().sharedMaterial=flame;spark.SetActive(false);embers[i]=spark.transform;
        }
        var light=new GameObject("Destello de explosión");light.transform.SetParent(transform,false);flash=light.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,.45f,.12f);flash.range=6;flash.shadows=LightShadows.None;flash.enabled=false;
    }
    public void Present(float fuse,bool arena)
    {
        bool exploding=fuse<=0;core.enabled=!exploding;wick.SetActive(!exploding);blast.enabled=exploding;burst.enabled=exploding;
        if(!exploding){flash.enabled=false;foreach(var ember in embers)ember.gameObject.SetActive(false);}
        if(!exploding)
        {
            transform.localScale=Vector3.one*.36f;
            var color=fuse<.5f&&Mathf.Sin(Time.time*30)>0?new Color(.7f,.1f,.035f):new Color(.055f,.065f,.07f);
            tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);core.SetPropertyBlock(tint);return;
        }
        float progress=Mathf.Clamp01(-fuse/BlastDuration),radius=BlastRadius*Mathf.Lerp(.8f,1,progress);
        burst.transform.position=transform.position+Vector3.up*.65f;
        burst.transform.rotation=Camera.main?Camera.main.transform.rotation:Quaternion.identity;
        burst.transform.localScale=Vector3.one*(BlastRadius*2/Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.x)));
        burstTint.SetFloat("_Progress",progress);burstTint.SetFloat("_Seed",Mathf.Repeat(transform.position.x*.73f+transform.position.z*.31f,19));burst.SetPropertyBlock(burstTint);
        flash.enabled=progress<.24f;flash.intensity=7*Mathf.Clamp01(1-progress/.24f);flash.transform.position=transform.position+Vector3.up*.6f;
        var right=Camera.main?Camera.main.transform.right:Vector3.right;
        for(int i=0;i<embers.Length;i++)
        {
            float angle=i*Mathf.PI*2/embers.Length;var direction=right*Mathf.Cos(angle)+Vector3.up*(Mathf.Abs(Mathf.Sin(angle))+.25f);
            embers[i].gameObject.SetActive(progress<.8f);embers[i].position=transform.position+direction*(BlastRadius*progress)+Vector3.up*(.25f-progress*progress*.9f);
        }
        blast.widthMultiplier=Mathf.Lerp(.2f,.035f,progress);
        for(int i=0;i<48;i++)
        {float a=i*Mathf.PI*2/48;var p=arena?new Vector3(Mathf.Cos(a)*radius,.08f,Mathf.Sin(a)*radius):new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,-.15f);blast.SetPosition(i,transform.position+p);}
    }
    public void Hide(){core.enabled=true;core.SetPropertyBlock(null);wick.SetActive(false);blast.enabled=false;burst.enabled=false;flash.enabled=false;foreach(var ember in embers)ember.gameObject.SetActive(false);}
}
