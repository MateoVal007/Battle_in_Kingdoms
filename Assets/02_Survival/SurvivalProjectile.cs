using UnityEngine;

namespace Kingdoms.Survival
{
    public sealed class SurvivalProjectile : MonoBehaviour
    {
        public SurvivalArena arena;
        public Vector3 velocity;
        public int owner,damage;
        public bool ice,dagger,fire,bomb;
        public float bombFuse=EnemyBombVisual.Fuse;
        EnemyBombVisual bombVisual;
        float remaining=3;
        public static SurvivalProjectile Create(SurvivalArena game,Vector3 position,Vector3 speed,int owner,int damage,bool frozen,bool dagger,bool fire=false,bool bomb=false)
        {
            var go=GameObject.CreatePrimitive(dagger?PrimitiveType.Cube:PrimitiveType.Sphere);
            go.name=bomb?"Bomba del lanzabombas":dagger?"Daga en vuelo":frozen?"Proyectil de hielo":"Proyectil mágico";
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            go.transform.position=position;go.transform.localScale=dagger?new Vector3(.08f,.08f,.6f):Vector3.one*.32f;
            go.GetComponent<Renderer>().sharedMaterial=dagger?game.steel:frozen?game.protection:fire?game.amber:game.basicSpell?game.basicSpell:game.amber;
            var bolt=go.AddComponent<SurvivalProjectile>();bolt.arena=game;bolt.owner=owner;bolt.damage=damage;bolt.ice=frozen;bolt.dagger=dagger;bolt.fire=fire;bolt.bomb=bomb;bolt.velocity=speed;
            if(bomb){bolt.bombVisual=go.AddComponent<EnemyBombVisual>();bolt.bombVisual.Present(bolt.bombFuse,true);}
            if(!dagger&&(frozen||fire))VillageSpellVisual.Attach(go,frozen,1);
            game.projectiles.Add(bolt);return bolt;
        }
        public void Tick(float dt)
        {
            if(bomb)
            {
                float before=bombFuse;
                if(before>0)EnemyBombVisual.Advance(transform,ref velocity,dt);
                bombFuse-=dt;
                if(before>0&&bombFuse<=0)
                {
                    velocity=Vector3.zero;
                    foreach(var hero in arena.heroes)
                        if(!hero.Down&&Vector3.Distance(hero.transform.position+Vector3.up*.7f,transform.position)<EnemyBombVisual.BlastRadius&&arena.ClearLine(transform.position,hero.transform.position+Vector3.up*.7f))
                            hero.Damage(damage,transform.position);
                }
                if(bombFuse< -EnemyBombVisual.BlastDuration)Remove();return;
            }
            var from=transform.position;var step=velocity*dt;
            foreach(var wall in Physics.SphereCastAll(from,.12f,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!wall.collider.GetComponentInParent<SurvivalHero>()&&!wall.collider.GetComponentInParent<SurvivalEnemy>()){Remove();return;}
            var to=from+step;
            if(owner>=0)
            {
                foreach(var foe in arena.enemies)
                    if(foe&&foe.health>0&&SegmentDistance(from,to,foe.transform.position+Vector3.up*(foe.Giant?1.8f:1))<(foe.Giant?1.2f:.65f))
                    {foe.Hit(damage,ice,from,true);Remove();return;}
            }
            else foreach(var hero in arena.heroes)
                if(!hero.Down&&SegmentDistance(from,to,hero.transform.position+Vector3.up)<.6f)
                {hero.Damage(damage,from);Remove();return;}
            transform.position=to;if(velocity.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(velocity);
            remaining-=dt;if(remaining<=0)Remove();
        }
        static float SegmentDistance(Vector3 a,Vector3 b,Vector3 p)
        {var d=b-a;float t=d.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0;return Vector3.Distance(p,a+d*t);}
        void Remove(){arena.projectiles.Remove(this);Destroy(gameObject);}
        void OnDestroy(){if(arena)arena.projectiles.Remove(this);}
        void LateUpdate()
        {
            if(bomb){if(!bombVisual)bombVisual=gameObject.AddComponent<EnemyBombVisual>();bombVisual.Present(bombFuse,true);return;}
            if(dagger||(!ice&&!fire)||!Camera.main)return;
            var core=GetComponentInChildren<MeshFilter>();if(!core||core.transform==transform)return;
            var camera=Camera.main.transform;var direction=new Vector2(Vector3.Dot(velocity,camera.right),Vector3.Dot(velocity,camera.up));
            core.transform.rotation=camera.rotation*Quaternion.AngleAxis(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg,Vector3.forward);
        }
    }
}
