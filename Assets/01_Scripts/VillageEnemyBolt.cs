using UnityEngine;
public class VillageEnemyBolt : MonoBehaviour
{
 public Vector3 velocity;public VillageEnemy owner;public bool arrow, dagger,bomb;public float bombFuse=EnemyBombVisual.Fuse;float age;
 EnemyBombVisual bombVisual;
 public static VillageEnemyBolt CreateBomb(VillageEnemy owner,Vector3 from,Vector3 target)
 {
  var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Bomba del lanzabombas";go.transform.position=from;
  var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);
  var bolt=go.AddComponent<VillageEnemyBolt>();bolt.owner=owner;bolt.bomb=true;bolt.velocity=EnemyBombVisual.ThrowVelocity(from,target);
  bolt.bombVisual=go.AddComponent<EnemyBombVisual>();bolt.bombVisual.Present(bolt.bombFuse,false);return bolt;
 }
 void Update()
 {
  if(!VillageOnline.CanSimulate)return;
  if(bomb)
  {
   float before=bombFuse;
   if(before>0)EnemyBombVisual.Advance(transform,ref velocity,Time.deltaTime);
   bombFuse-=Time.deltaTime;
   if(before>0&&bombFuse<=0)
   {
    velocity=Vector3.zero;
    foreach(var hero in FindObjectsByType<VillageHero>())
     if(!hero.IsDown&&Vector3.Distance(hero.transform.position,transform.position)<EnemyBombVisual.BlastRadius&&ClearBlast(hero.transform.position))
      hero.Damage(owner?owner.damage:12,true,transform.position.x,transform.position.y);
   }
   if(!bombVisual)bombVisual=gameObject.AddComponent<EnemyBombVisual>();bombVisual.Present(bombFuse,false);
   if(bombFuse< -EnemyBombVisual.BlastDuration)Destroy(gameObject);
   return;
  }
  if (arrow || dagger) StyleProjectile(transform, velocity, dagger);
  float distance=velocity.magnitude*Time.deltaTime;
  var hits=Physics.SphereCastAll(transform.position,.1f,velocity.normalized,distance,~0,QueryTriggerInteraction.Ignore);
  System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
  foreach(var hit in hits)
  {
   if(hit.collider.GetComponentInParent<VillageEnemy>())continue;
   var hero=hit.collider.GetComponentInParent<VillageHero>();
   if(hero){var source=hero.transform.position-velocity.normalized*3;hero.Damage(dagger && owner ? owner.damage : 12,true,source.x,source.y);}
   Destroy(gameObject);return;
  }
  transform.position+=velocity*Time.deltaTime;if((age+=Time.deltaTime)>4)Destroy(gameObject);
 }
 bool ClearBlast(Vector3 target)
 {
  foreach(var hit in Physics.RaycastAll(transform.position,(target-transform.position).normalized,Vector3.Distance(transform.position,target),~0,QueryTriggerInteraction.Ignore))
   if(!hit.collider.GetComponentInParent<VillageEnemy>()&&!hit.collider.GetComponentInParent<VillageHero>())return false;
  return true;
 }
 public static void StyleProjectile(Transform projectile, Vector3 velocity, bool dagger)
 {
  projectile.localScale = dagger ? new Vector3(.54f, .19f, .13f) : new Vector3(.7f, .09f, .09f);
  projectile.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
 }
}
