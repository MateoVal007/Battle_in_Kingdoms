using UnityEngine;
using UnityEngine.AI;

namespace Kingdoms.Survival
{
    public sealed class SurvivalEnemy : MonoBehaviour
    {
        public SurvivalArena arena;
        public FoeKind kind;
        public int id,health,maxHealth,shieldHealth,attackSerial;
        public Vector3 heading=Vector3.back;
        public bool preparing;
        public float windupRemaining,moveSpeed,frozenUntil;
        public NavMeshAgent agent;
        public SurvivalEnemyView view;
        float cooldown,repath,vanishAt;
        SurvivalHero target;int spawnRound;
        public int ContactDamage=>Giant?28:(kind==FoeKind.Runner?8:kind==FoeKind.Goblin?9:10)+Mathf.Clamp(spawnRound-1,0,4);
        public int ProjectileDamage=>Bomber?12+Mathf.Clamp(spawnRound-1,0,4):9+Mathf.Clamp(spawnRound-2,0,3);
        Vector3 strikePoint,frozenPosition;bool freezeLocked;
        public const float FreezeDuration=3f;
        public bool CanFreeze=>!Giant&&(kind!=FoeKind.Dagger||shieldHealth<=0);
        public bool IsFrozen=>health>0&&CanFreeze&&Time.time<frozenUntil;
        public float FrozenRemaining=>IsFrozen?Mathf.Max(0,frozenUntil-Time.time):0;
        public void ApplyRemoteFreeze(float remaining){frozenUntil=Time.time+Mathf.Max(0,remaining);}
        public bool Giant=>kind==FoeKind.Giant;
        public bool Bomber=>kind==FoeKind.Bomber;
        public void Initialize(int wave)
        {
            spawnRound=Mathf.Clamp(wave,1,5);
            maxHealth=Giant?720:kind==FoeKind.Dagger?70:Bomber?55:kind==FoeKind.Goblin?46:kind==FoeKind.Runner?48:60;health=maxHealth;
            shieldHealth=kind==FoeKind.Dagger?60:0;
            agent=gameObject.AddComponent<NavMeshAgent>();agent.radius=Giant?.9f:.4f;agent.height=Giant?3.8f:1.85f;
            agent.speed=Giant?2.6f:kind==FoeKind.Runner?4.4f:2.7f+(wave-1)*.12f;
            agent.acceleration=16;agent.stoppingDistance=Giant?2.8f:Bomber?7:kind==FoeKind.Dagger?6:kind==FoeKind.Goblin?1.3f:1.65f;agent.updateRotation=false;
            view=gameObject.AddComponent<SurvivalEnemyView>();view.enemy=this;view.Initialize();
        }
        void Update(){if(health<=0&&vanishAt>0&&Time.time>=vanishAt)gameObject.SetActive(false);else if(freezeLocked&&!IsFrozen&&health>0)Thaw();}
        void Thaw()
        {
            if(agent&&agent.enabled&&agent.isOnNavMesh){agent.Warp(frozenPosition);agent.updatePosition=true;}
            freezeLocked=false;
        }
        public void Tick(float dt)
        {
            if(health<=0||!agent||!agent.enabled||!agent.isOnNavMesh)return;
            if(IsFrozen){agent.isStopped=true;agent.nextPosition=frozenPosition;transform.position=frozenPosition;moveSpeed=0;return;}
            if(freezeLocked)Thaw();
            SurvivalHero nearest=null;float closest=float.PositiveInfinity;
            foreach(var hero in arena.heroes)
            {
                if(!hero||hero.Down)continue;
                float distanceSquared=(hero.transform.position-transform.position).sqrMagnitude;
                if(distanceSquared<closest){closest=distanceSquared;nearest=hero;}
            }
            // Reconsider both players while chasing; a wind-up still commits to its advertised strike.
            if(target!=nearest){target=nearest;repath=0;}
            if(!target){agent.isStopped=true;return;}
            cooldown-=dt;repath-=dt;
            Vector3 offset=target.transform.position-transform.position;
            var flat=Vector3.ProjectOnPlane(offset,Vector3.up);
            if(flat.sqrMagnitude>.05f&&!preparing)heading=flat.normalized;
            float distance=offset.magnitude,range=Giant?4.1f:Bomber?12:kind==FoeKind.Dagger?9:kind==FoeKind.Goblin?1.95f:2.25f;
            agent.speed=(Giant?2.6f:kind==FoeKind.Runner?4.4f:3.1f);
            if(Bomber)SteerBomber(flat);
            if(preparing)
            {
                if(!Bomber)agent.isStopped=true;windupRemaining-=dt;
                if(windupRemaining<=0){preparing=false;Strike();cooldown=Giant?2.5f:Bomber?2.1f:kind==FoeKind.Dagger?1.65f:1.1f;}
            }
            else if(distance<range&&cooldown<=0&&Mathf.Abs(offset.y)<(kind==FoeKind.Dagger||Bomber?8:1.7f)&&arena.ClearLine(transform.position+Vector3.up*1.2f,target.transform.position+Vector3.up))
            {
                preparing=true;windupRemaining=Giant?1.2f:kind==FoeKind.Dagger||Bomber?.65f:.5f;
                strikePoint=Giant?transform.position+heading*2.4f:target.transform.position;attackSerial++;if(!Bomber)agent.isStopped=true;
            }
            else if(!Bomber)
            {
                agent.isStopped=cooldown>(Giant?1.8f:.75f);moveSpeed=agent.isStopped?0:agent.velocity.magnitude;
                if(repath<=0&&!agent.isStopped)
                {
                    repath=.22f;
                    var goal=target.transform.position;
                    if(kind==FoeKind.Dagger&&flat.magnitude<4)goal=transform.position-heading*4;
                    if(NavMesh.SamplePosition(goal,out var hit,2,NavMesh.AllAreas))agent.SetDestination(hit.position);
                }
            }
        }
        void SteerBomber(Vector3 offset)
        {
            bool retreat=offset.magnitude<6,approach=offset.magnitude>9.5f;
            agent.isStopped=!retreat&&!approach;moveSpeed=agent.isStopped?0:agent.velocity.magnitude;
            if(agent.isStopped||repath>0)return;
            repath=.25f;agent.stoppingDistance=retreat?.15f:7;
            if(!retreat)
            {if(NavMesh.SamplePosition(target.transform.position,out var hit,2,NavMesh.AllAreas))agent.SetDestination(hit.position);return;}
            var away=-offset.normalized;var tangent=Vector3.Cross(Vector3.up,away);
            Vector3 best=transform.position;float score=offset.sqrMagnitude;
            foreach(var direction in new[]{away,(away+tangent).normalized,(away-tangent).normalized,tangent,-tangent})
            {
                if(!NavMesh.SamplePosition(transform.position+direction*4,out var hit,1.5f,NavMesh.AllAreas))continue;
                var path=new NavMeshPath();float distance=(hit.position-target.transform.position).sqrMagnitude;
                if(distance>score&&agent.CalculatePath(hit.position,path)&&path.status==NavMeshPathStatus.PathComplete){score=distance;best=hit.position;}
            }
            agent.SetDestination(best);
        }
        void Strike()
        {
            if(Bomber)
            {
                var origin=transform.position+Vector3.up*1.25f+heading*.6f;
                SurvivalProjectile.Create(arena,origin,EnemyBombVisual.ThrowVelocity(origin,strikePoint+Vector3.up*.15f),-1,ProjectileDamage,false,false,false,true);
            }
            else if(kind==FoeKind.Dagger)
            {
                var origin=transform.position+Vector3.up*1.15f+heading*.7f;
                SurvivalProjectile.Create(arena,origin,(strikePoint+Vector3.up-origin).normalized*12,-1,ProjectileDamage,false,true);
            }
            else foreach(var hero in arena.heroes)
            {
                if(hero.Down)continue;
                var delta=hero.transform.position-(Giant?strikePoint:transform.position);
                if(Mathf.Abs(delta.y)>1.7f)continue;
                delta.y=0;
                if(delta.magnitude<(Giant?3.6f:2.4f)&&arena.ClearLine(transform.position+Vector3.up,hero.transform.position+Vector3.up))
                    if(Giant||Vector3.Dot(delta.normalized,heading)>0)hero.Damage(ContactDamage,transform.position,!Giant);
            }
        }
        public void Hit(int amount,bool frozen,Vector3 origin,bool projectile)
        {
            if(health<=0)return;
            var from=Vector3.ProjectOnPlane(origin-transform.position,Vector3.up).normalized;
            if(projectile&&shieldHealth>0&&Vector3.Dot(from,heading)>.1f)
            {shieldHealth=Mathf.Max(0,shieldHealth-amount);if(shieldHealth==0)arena.notice="Escudo del lanzadagas destruido";return;}
            health=Mathf.Max(0,health-amount);
            if(frozen&&health>0&&CanFreeze)
            {
                frozenUntil=Time.time+FreezeDuration;preparing=false;windupRemaining=moveSpeed=0;cooldown=Mathf.Max(cooldown,.2f);
                frozenPosition=transform.position;freezeLocked=true;
                if(agent&&agent.enabled&&agent.isOnNavMesh){agent.isStopped=true;agent.updatePosition=false;agent.nextPosition=frozenPosition;}
            }
            if(health==0)
            {
                preparing=false;if(agent&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;
                vanishAt=Time.time+1.4f;arena.EnemyDefeated(this);
            }
        }
    }
}
