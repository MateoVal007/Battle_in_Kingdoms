using UnityEngine;

// Shared presentation for both modes; meshes, bones and shared materials remain unchanged.
[DisallowMultipleComponent]
public sealed class FrozenEnemyVisual : MonoBehaviour
{
    public static readonly Color IceTint=new Color(.28f,.88f,1f,1);
    Renderer[] parts;Animator[] animators;MaterialPropertyBlock[] previous;float[] speeds;
    MaterialPropertyBlock ice;
    public bool Applied {get;private set;}
    void Cache()
    {
        parts=GetComponentsInChildren<Renderer>(true);animators=GetComponentsInChildren<Animator>(true);
        previous=new MaterialPropertyBlock[parts.Length];speeds=new float[animators.Length];
        for(int i=0;i<previous.Length;i++)previous[i]=new MaterialPropertyBlock();
        ice=new MaterialPropertyBlock();ice.SetColor("_BaseColor",IceTint);ice.SetColor("_Color",IceTint);
    }
    public void Present(bool frozen)
    {
        if(parts==null)Cache();
        if(frozen)
        {
            if(!Applied)
            {
                for(int i=0;i<parts.Length;i++)if(parts[i])parts[i].GetPropertyBlock(previous[i]);
                for(int i=0;i<animators.Length;i++)if(animators[i])speeds[i]=animators[i].speed;
            }
            Applied=true;
            foreach(var part in parts)if(part)part.SetPropertyBlock(ice);
            foreach(var animator in animators)if(animator)animator.speed=0;
        }
        else if(Applied)
        {
            for(int i=0;i<parts.Length;i++)if(parts[i])parts[i].SetPropertyBlock(previous[i]);
            for(int i=0;i<animators.Length;i++)if(animators[i])animators[i].speed=speeds[i];
            Applied=false;
        }
    }
    void OnDisable(){if(Applied)Present(false);}
}
