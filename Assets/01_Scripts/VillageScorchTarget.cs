using UnityEngine;

[ExecuteAlways]
public class VillageScorchTarget : MonoBehaviour
{
    public Vector3 burnLocalOffset = new Vector3(0.4f, 2.2f, -1.2f);
    public float burnRadius = 3.8f;
    [Range(0f, 1f)] public float burnIntensity = 0.85f;
    [Range(0.5f, 4f)] public float sootRise = 1.9f;
    [Range(0f, 2f)] public float emberStrength = 0.65f;

    static readonly int BurnCenterId = Shader.PropertyToID("_BurnCenter");
    static readonly int BurnRadiusId = Shader.PropertyToID("_BurnRadius");
    static readonly int BurnIntensityId = Shader.PropertyToID("_BurnIntensity");
    static readonly int SootRiseId = Shader.PropertyToID("_SootRise");
    static readonly int EmberStrengthId = Shader.PropertyToID("_EmberStrength");

    MaterialPropertyBlock block;

    void OnEnable() => Apply();
    void OnValidate() => Apply();
    void Update()
    {
        if (!Application.isPlaying) Apply();
    }

    public void Apply()
    {
        if (block == null) block = new MaterialPropertyBlock();
        Vector3 worldCenter = transform.TransformPoint(burnLocalOffset);
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetVector(BurnCenterId, new Vector4(worldCenter.x, worldCenter.y, worldCenter.z, 1f));
            block.SetFloat(BurnRadiusId, burnRadius);
            block.SetFloat(BurnIntensityId, burnIntensity);
            block.SetFloat(SootRiseId, sootRise);
            block.SetFloat(EmberStrengthId, emberStrength);
            r.SetPropertyBlock(block);
        }
    }
}
