using UnityEngine;

// Shared collectible: mask stores the first collector (1 warrior, 2 mage).
public class VillageStar:MonoBehaviour
{
 public const int Capacity=512;
 public int id,mask;
 public Transform visual;
 Renderer[] bodies;
 Quaternion baseRotation;
 Vector3 basePosition;
 void Awake(){bodies=visual?visual.GetComponentsInChildren<Renderer>(true):GetComponentsInChildren<Renderer>(true);if(visual){basePosition=visual.localPosition;baseRotation=visual.localRotation;}}
 void OnTriggerEnter(Collider c){TryCollect(c.GetComponentInParent<VillageHero>());}
 void OnTriggerStay(Collider c){TryCollect(c.GetComponentInParent<VillageHero>());}
 public bool TryCollect(VillageHero hero)
 {
  if(!VillageOnline.CanSimulate||!hero||hero.IsDown)return false;
  if(mask!=0)return false;
  mask=hero.isMage?2:1;hero.AddStars(1);return true;
 }
 public void ApplyMask(int value){mask=value;}
 void LateUpdate()
 {
  foreach(var body in bodies)if(body)body.enabled=mask==0;
  if(visual){visual.localPosition=basePosition+Vector3.up*(Mathf.Sin(Time.time*2.5f+id)*.08f);visual.localRotation=baseRotation*Quaternion.Euler(0,(Time.time*100+id*31)%360,0);}
 }
}
