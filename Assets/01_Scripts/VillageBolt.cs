using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class VillageBolt:MonoBehaviour {
 public float direction;public int damage;public bool ice;float age;
 void Update(){
 foreach(var c in Physics.OverlapSphere(transform.position,.18f,~0,QueryTriggerInteraction.Ignore)){
  if(c.gameObject==gameObject||c.GetComponentInParent<VillageHero>())continue;
  var chest=c.GetComponentInParent<VillageChest>();if(chest)chest.Hit();
  var enemy=c.GetComponentInParent<VillageEnemy>();if(enemy)enemy.HitProjectile(damage,ice,direction);
  GetComponent<VillageSpellVisual>()?.Impact();Destroy(gameObject);return;
 }
 float step=14*Time.deltaTime;var from=transform.position;var hits=Physics.RaycastAll(from,Vector3.right*direction,step+.2f);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var hit in hits){if(hit.collider.isTrigger||hit.collider.GetComponentInParent<VillageHero>())continue;var chest=hit.collider.GetComponentInParent<VillageChest>();if(chest)chest.Hit();var e=hit.collider.GetComponentInParent<VillageEnemy>();if(e)e.HitProjectile(damage,ice,direction);GetComponent<VillageSpellVisual>()?.Impact();Destroy(gameObject);return;}transform.position+=Vector3.right*direction*step;age+=Time.deltaTime;if(age>2)Destroy(gameObject);}
}



