using UnityEngine;

// Presentation only: VillageBolt retains all projectile movement, damage and freezing logic.
public sealed class VillageSpellVisual : MonoBehaviour
{
    static Mesh quad;
    [SerializeField] ParticleSystem sparks;
    [SerializeField] bool ice;
    bool impacted;

    public static void Attach(GameObject projectile, bool frozen, float direction)
    {
        var material = Resources.Load<Material>(frozen ? "VFX/MageIce" : "VFX/MageFire");
        var sparkMaterial = Resources.Load<Material>(frozen ? "VFX/IceSpark" : "VFX/FireSpark");
        if (!material || !sparkMaterial) return;
        var effect = projectile.AddComponent<VillageSpellVisual>();
        effect.ice = frozen;
        var old = projectile.GetComponent<Renderer>();
        if (old) old.enabled = false;
        if (!quad)
        {
            quad = new Mesh { name = "Spell quad" };
            quad.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.triangles = new[] { 0,2,1,0,3,2 };
            quad.RecalculateBounds();
        }
        var core = new GameObject(frozen ? "Hielo · núcleo y escarcha" : "Fuego · llama y halo");
        core.transform.SetParent(projectile.transform,false);
        var scale = projectile.transform.lossyScale;
        core.transform.localScale = new Vector3(2.6f/scale.x,1.25f/scale.y,1/scale.z);
        core.AddComponent<MeshFilter>().sharedMesh = quad;
        var renderer = core.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var properties = new MaterialPropertyBlock();
        properties.SetFloat("_Facing",direction < 0 ? -1 : 1);
        properties.SetFloat("_Seed",Random.value*50);
        renderer.SetPropertyBlock(properties);
        var wake = new GameObject(frozen ? "Estela · cristales de hielo" : "Estela · brasas");
        wake.transform.SetParent(projectile.transform,false);
        wake.transform.localScale = new Vector3(1/scale.x,1/scale.y,1/scale.z);
        effect.sparks = wake.AddComponent<ParticleSystem>();
        effect.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = effect.sparks.main;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.16f,.30f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.15f,.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(.035f,frozen ? .095f : .075f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
        main.startColor = frozen ? new Color(.35f,.85f,1) : new Color(1,.42f,.045f);
        main.maxParticles = 48;
        main.gravityModifier = frozen ? .025f : -.025f;
        var emission = effect.sparks.emission; emission.rateOverTime = 40;
        var shape = effect.sparks.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .14f;
        var colors = effect.sparks.colorOverLifetime; colors.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(frozen?new Color(.2f,.65f,1):new Color(1,.12f,.015f),1)},new[]{new GradientAlphaKey(.85f,0),new GradientAlphaKey(0,1)});
        colors.color = gradient;
        var sizes = effect.sparks.sizeOverLifetime; sizes.enabled = true; sizes.size = new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        var pr = effect.sparks.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = sparkMaterial; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;
        effect.sparks.Play();
    }

    public void Impact()
    {
        if (impacted || !sparks) return;
        impacted = true;
        sparks.transform.SetParent(null,true);
        var emission = sparks.emission; emission.enabled = false;
        sparks.Emit(ice ? 18 : 24);
        Destroy(sparks.gameObject,.7f);
    }
}
