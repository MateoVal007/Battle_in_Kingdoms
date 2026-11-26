using UnityEngine;
public class VillageAmbient : MonoBehaviour
{
    public Vector3 rotation;
    public float bob;
    Vector3 origin;
    void Start() { origin = transform.localPosition; }
    void Update()
    {
        transform.Rotate(rotation * Time.deltaTime, Space.Self);
        if (bob > 0) transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * 2 + origin.x) * bob);
    }
}
