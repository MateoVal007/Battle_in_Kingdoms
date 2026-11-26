using UnityEngine;
using UnityEngine.AI;

namespace Kingdoms.Survival
{
    public sealed class SurvivalHero : MonoBehaviour
    {
        public SurvivalArena arena;
        public int id,health=100,armor,stars,lives=3,selectedSpell,attackSerial;
        public float fire=12,ice=12,protection=25,immuneUntil,attackAge=10;
        public bool equipped,guarding,protectedByMagic;
        public Vector3 heading=Vector3.forward;
        public float moveSpeed;
        public bool airborne;
        public float verticalVelocity;
        public float GroundY {get;private set;}
        public bool Down=>health<=0;
        public bool IsMage=>id==1;
        public NavMeshAgent agent;
        public SurvivalHeroView view;
        CharacterController jumpMotor;
        Vector3 spawn,deathPosition;bool hasDeathPosition;
        public Vector3 DeathPosition=>hasDeathPosition?deathPosition:transform.position;
        Vector3 airGoal;
        bool airHasGoal;
        int clickSerial,cycleSerial,useSerial,lifeSerial,jumpSerial;
        float attackCooldown,hitDelay;
        bool meleePending;

        public void Initialize()
        {
            if(agent)return;
            spawn=transform.position;
            agent=gameObject.AddComponent<NavMeshAgent>();agent.radius=.4f;agent.height=1.9f;agent.speed=5.5f;agent.acceleration=24;agent.angularSpeed=720;agent.stoppingDistance=.12f;agent.updateRotation=false;agent.updatePosition=false;
            jumpMotor=gameObject.AddComponent<CharacterController>();jumpMotor.height=1.9f;jumpMotor.center=Vector3.up*.95f;jumpMotor.radius=.36f;jumpMotor.skinWidth=.025f;jumpMotor.stepOffset=.3f;jumpMotor.slopeLimit=50;GroundY=transform.position.y;
            jumpMotor.Move(Vector3.down*.03f);
            view=gameObject.AddComponent<SurvivalHeroView>();view.hero=this;view.Initialize();
        }
        public void Tick(SurvivalCommand input,float dt)
        {
            if(!agent)return;
            if(input.LifeSerial!=lifeSerial){lifeSerial=input.LifeSerial;if(UseLife()){clickSerial=input.ClickSerial;moveSpeed=0;return;}}
            if(Down){guarding=false;moveSpeed=0;jumpSerial=input.JumpSerial;if(airborne)MoveAir(default,dt);if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;return;}
            attackAge+=dt;attackCooldown=Mathf.Max(0,attackCooldown-dt);
            if(input.CycleSerial!=cycleSerial)
            {selectedSpell=(selectedSpell+(input.CycleSerial-cycleSerial)%3+3)%3;cycleSerial=input.CycleSerial;protectedByMagic=false;}
            bool use=input.UseSerial!=useSerial;useSerial=input.UseSerial;
            if(protectedByMagic){protection=Mathf.Max(0,protection-dt);if(protection<=0)protectedByMagic=false;}
            if(input.JumpSerial!=jumpSerial){jumpSerial=input.JumpSerial;if(!airborne&&jumpMotor.isGrounded&&arena.phase!=ArenaPhase.Victory)BeginJump();}
            guarding=!IsMage&&equipped&&input.Guard&&!airborne;
            bool stopped=guarding||attackAge<.18f||arena.phase==ArenaPhase.Victory;
            if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=stopped;
            if(airborne)MoveAir(input,dt);
            else MoveGround(input,dt,stopped);
            if(!airborne)GroundY=transform.position.y;
            Vector3 desired=input.Move.sqrMagnitude>.01f?new Vector3(input.Move.x,0,input.Move.y):airborne?(airHasGoal?airGoal-transform.position:Vector3.zero):agent.enabled?agent.desiredVelocity:Vector3.zero;
            desired.y=0;
            if(airborne)moveSpeed=stopped?0:desired.normalized.magnitude*5.5f;
            if(desired.sqrMagnitude>.02f&&attackAge>.45f)heading=Vector3.ProjectOnPlane(desired,Vector3.up).normalized;
            bool special=use&&IsMage;
            if(special&&selectedSpell==2)
                protectedByMagic=!protectedByMagic&&protection>0;
            else if(special&&attackCooldown<=0)
            {
                bool charged=selectedSpell==0?fire>0:ice>0;
                if(charged){Face(input);if(selectedSpell==0)fire--;else ice--;attackSerial++;attackAge=0;attackCooldown=.38f;Cast(input,selectedSpell==1,selectedSpell==0);}
                else arena.notice="Sin cargas de "+VillageHero.ItemNames[selectedSpell]+" · busca otro libro por el mapa";
            }
            else if(input.Attack&&!special&&attackCooldown<=0)
            {
                Face(input);
                attackSerial++;attackAge=0;attackCooldown=IsMage?.38f:equipped?.52f:.35f;
                if(IsMage)Cast(input,false,false);else {meleePending=true;hitDelay=.14f;}
            }
            if(meleePending)
            {
                hitDelay-=dt;
                if(hitDelay<=0)
                {
                    meleePending=false;
                    foreach(var enemy in arena.enemies)
                    {
                        if(!enemy||enemy.health<=0)continue;
                        var d=enemy.transform.position-transform.position;
                        if(Mathf.Abs(d.y)>(enemy.Giant?3:1.8f))continue;d.y=0;
                        if(d.magnitude<(equipped?2.7f:1.8f)&&Vector3.Dot(d.normalized,heading)>.25f&&arena.ClearLine(transform.position+Vector3.up,enemy.transform.position+Vector3.up))
                            enemy.Hit(equipped?28:12,false,transform.position,false);
                    }
                }
            }
        }
        void Face(SurvivalCommand input)
        {
            if(!input.HasAim)return;var aim=input.Aim-transform.position;aim.y=0;if(aim.sqrMagnitude>.05f)heading=aim.normalized;
        }
        void Cast(SurvivalCommand input,bool frozen,bool flame)
        {
            Vector3 from=transform.position+Vector3.up*1.15f+heading*.7f;
            Vector3 aim=input.HasAim?input.Aim+Vector3.up:from+heading*30;
            var direction=aim-from;if(Vector3.Dot(Vector3.ProjectOnPlane(direction,Vector3.up),heading)<.1f)direction=heading;
            SurvivalProjectile.Create(arena,from,direction.normalized*16,id,flame?35:20,frozen,false,flame);
        }
        void BeginJump()
        {
            GroundY=transform.position.y;airHasGoal=agent.enabled&&agent.isOnNavMesh&&agent.hasPath;airGoal=airHasGoal?agent.destination:transform.position;
            agent.enabled=false;jumpMotor.enabled=true;airborne=true;verticalVelocity=9.8f;
        }
        void MoveGround(SurvivalCommand input,float dt,bool stopped)
        {
            if(!jumpMotor.enabled)jumpMotor.enabled=true;
            if(!agent.enabled)AttachPlanner();
            bool planning=agent.enabled&&agent.isOnNavMesh;
            if(planning)agent.nextPosition=transform.position;
            if(input.ClickSerial!=clickSerial)
            {clickSerial=input.ClickSerial;if(planning&&NavMesh.SamplePosition(input.Destination,out var hit,2,NavMesh.AllAreas))agent.SetDestination(hit.position);}
            Vector3 move=Vector3.zero;
            if(!stopped)
            {
                if(input.Move.sqrMagnitude>.01f){if(planning)agent.ResetPath();move=new Vector3(input.Move.x,0,input.Move.y).normalized*5.5f;}
                else if(planning&&agent.hasPath&&!agent.pathPending&&agent.remainingDistance>agent.stoppingDistance+.02f)
                    move=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(agent.desiredVelocity,Vector3.up),5.5f);
            }
            verticalVelocity=jumpMotor.isGrounded?-2:Mathf.Max(-24,verticalVelocity-24*dt);
            var before=transform.position;var flags=jumpMotor.Move((move+Vector3.up*verticalVelocity)*dt);
            moveSpeed=Vector3.ProjectOnPlane(transform.position-before,Vector3.up).magnitude/Mathf.Max(.0001f,dt);
            if((flags&CollisionFlags.Below)==0&&!jumpMotor.isGrounded)
            {
                // The navigation mesh plans click paths; physical support decides whether we fall.
                airborne=true;airHasGoal=planning&&agent.hasPath;airGoal=airHasGoal?agent.destination:transform.position;
                if(agent.enabled)agent.enabled=false;
            }
            else if(planning)agent.nextPosition=transform.position;
        }
        void AttachPlanner()
        {
            var physical=transform.position;
            if(!NavMesh.SamplePosition(physical,out var hit,.9f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-physical.y)>.35f)return;
            jumpMotor.enabled=false;transform.position=hit.position;agent.enabled=true;agent.Warp(hit.position);
            // Keep the exact physical landing position, including the edge overhang.
            transform.position=physical;jumpMotor.enabled=true;
        }
        void MoveAir(SurvivalCommand input,float dt)
        {
            if(input.ClickSerial!=clickSerial){clickSerial=input.ClickSerial;airGoal=input.Destination;airHasGoal=true;}
            Vector3 move=new Vector3(input.Move.x,0,input.Move.y);
            if(move.sqrMagnitude>.01f)airHasGoal=false;
            else if(airHasGoal){move=Vector3.ProjectOnPlane(airGoal-transform.position,Vector3.up);if(move.magnitude<.2f)move=Vector3.zero;}
            verticalVelocity-=24*dt;
            var flags=jumpMotor.Move(move.normalized*5.5f*dt+Vector3.up*(verticalVelocity*dt));
            if((flags&CollisionFlags.Above)!=0&&verticalVelocity>0)verticalVelocity=0;
            if(verticalVelocity<=0&&(jumpMotor.isGrounded||(flags&CollisionFlags.Below)!=0))Land();
            if(transform.position.y< -8){Land();Revive(arena.SafePoint(spawn),Mathf.Max(1,health));}
        }
        void Land()
        {
            airborne=false;verticalVelocity=0;
            var at=transform.position;AttachPlanner();GroundY=at.y;
            if(airHasGoal&&!Down&&agent.enabled&&agent.isOnNavMesh)agent.SetDestination(airGoal);
            foreach(var e in arena.enemies)
                if(e&&e.health>0&&Mathf.Abs(e.transform.position.y-at.y)<1&&Vector3.Distance(e.transform.position,at)<.85f)
                    Damage(e.ContactDamage,e.transform.position,false);
        }
        public bool Damage(int damage,Vector3 source,bool blockable=true)
        {
            if(Down||Time.time<immuneUntil||protectedByMagic)return false;
            var incoming=Vector3.ProjectOnPlane(source-transform.position,Vector3.up).normalized;
            if(blockable&&guarding&&equipped&&Vector3.Dot(incoming,heading)>.15f)
            {armor=Mathf.Max(0,armor-Mathf.Max(1,damage/2));immuneUntil=Time.time+.18f;if(armor==0)equipped=false;view?.Impact();return false;}
            int absorbed=equipped?Mathf.Min(armor,damage/2):0;armor-=absorbed;health=Mathf.Max(0,health-damage+absorbed);
            if(armor==0)equipped=false;immuneUntil=Time.time+3;
            if(Down)
            {
                deathPosition=arena.SafePoint(transform.position);hasDeathPosition=true;
                meleePending=guarding=protectedByMagic=false;airborne=airHasGoal=false;verticalVelocity=0;
                if(jumpMotor)jumpMotor.enabled=false;
                if(agent&&arena.Authoritative){if(!agent.enabled)agent.enabled=true;agent.Warp(deathPosition);transform.position=deathPosition;agent.ResetPath();agent.isStopped=true;}
                arena.notice="Jugador derribado · T revive aquí con toda la salud · cuesta 1 vida";arena.CheckDefeat();
            }
            return true;
        }
        public void AddStars(int amount){stars+=amount;while(stars>=100){stars-=100;lives++;arena.notice="100 estrellas · +1 vida propia";}}
        public bool UseLife(){if(arena.phase==ArenaPhase.Defeat||arena.TeamWiped||!Down||lives<=0)return false;lives--;Revive(arena.SafePoint(DeathPosition),100);return true;}
        public void Revive(Vector3 at,int hp)
        {
            health=hp;hasDeathPosition=false;immuneUntil=Time.time+3;airborne=false;verticalVelocity=0;airHasGoal=false;GroundY=at.y;
            if(jumpMotor)jumpMotor.enabled=false;
            if(agent){if(!agent.enabled&&arena.Authoritative)agent.enabled=true;if(agent.enabled){agent.Warp(at);agent.ResetPath();agent.isStopped=false;clickSerial=arena.commands[id].ClickSerial;}else transform.position=at;}
            else transform.position=at;
            transform.position=at;if(jumpMotor)jumpMotor.enabled=true;
        }
        public void ResetHero()
        {
            health=100;armor=stars=0;lives=3;selectedSpell=0;equipped=guarding=protectedByMagic=false;fire=ice=12;protection=25;
            attackAge=10;attackCooldown=hitDelay=moveSpeed=0;meleePending=false;
            var cmd=arena.commands[id];clickSerial=cmd.ClickSerial;cycleSerial=cmd.CycleSerial;useSerial=cmd.UseSerial;lifeSerial=cmd.LifeSerial;jumpSerial=cmd.JumpSerial;
            Revive(spawn,100);
        }
    }
}

