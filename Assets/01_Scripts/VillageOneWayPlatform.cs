using UnityEngine;
[DefaultExecutionOrder(-100)]
public class VillageOneWayPlatform : MonoBehaviour
{
    Collider surface;
    void Awake(){surface=GetComponent<Collider>();}
    void Update()
    {
        foreach(var hero in FindObjectsByType<VillageHero>())
        {
            var motor=hero.GetComponent<CharacterController>();
            float feet=hero.transform.position.y+motor.center.y-motor.height*.5f;
            bool pass=hero.VerticalSpeed>.05f || feet<surface.bounds.max.y-.18f;
            Physics.IgnoreCollision(surface,motor,false); // Platforms are solid on all sides.
        }
    }
}
