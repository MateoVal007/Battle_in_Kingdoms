using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Kingdoms.Survival
{
    public enum ArenaPhase { Preparation, Wave, Rest, Victory, Defeat }
    public enum FoeKind { Soldier, Dagger, Runner, Giant, Goblin, Bomber }
    public enum SupplyKind { Star, Armor, Fire, Ice, Protection, WarriorLife, MageLife, Health }

    // Separate 3D rules: the platform controller and its collision plane remain untouched.
    public sealed class SurvivalArena : MonoBehaviour
    {
        public static SurvivalArena Instance { get; private set; }
        public SurvivalHero[] heroes;
        public SurvivalPickup[] pickups;
        public SurvivalCamera sharedCamera;
        public NavMeshData navigation;
        public Transform exitGate;
        public Material stone, steel, amber, warning, protection,basicSpell;
        public SurvivalInputReader input {get;private set;}
        public Vector3[] entrances;
        public int[] waveSizes = { 4, 6, 8, 10, 12 };
        public int maxSimultaneous = 6;
        public float restSeconds = 30, spawnInterval = 1.4f;
        public ArenaPhase phase = ArenaPhase.Preparation;
        public int wave, pending, totalKills, activeHero;
        public float countdown = 30;
        public string notice = "Explora la plaza y las galerías · suministros repartidos por el mapa";
        public bool externalSimulation;
        public readonly List<SurvivalEnemy> enemies = new List<SurvivalEnemy>();
        public readonly List<SurvivalProjectile> projectiles = new List<SurvivalProjectile>();
        public readonly SurvivalCommand[] commands = new SurvivalCommand[2];
        public bool Authoritative => !SurvivalSession.Instance || !SurvivalSession.Instance.Connected || SurvivalSession.Instance.IsHost;
        public bool Running => !SurvivalSession.Instance || (SurvivalSession.Instance.Connected ? SurvivalSession.Instance.PlayerCount == 2 : !SurvivalSession.Instance.onlineRequested);
        public int LivingEnemies => enemies.Count(e => e && e.health > 0);
        public int Remaining => LivingEnemies + pending;
        public SurvivalHero Selected => heroes[Mathf.Clamp(activeHero,0,1)];
        public int SupplyRound=>Mathf.Clamp(phase==ArenaPhase.Rest?wave+1:wave,1,5);
        public int HealthRestore=>SurvivalSupplyRules.HealthGain(SupplyRound);
        NavMeshDataInstance navInstance;
        float nextSpawn, pickupClock;
        int entranceIndex;
        public const float DefeatRestartSeconds=3;
        public bool TeamWiped=>heroes!=null&&heroes.Length==2&&heroes.All(h=>h&&h.Down);
        public bool CheckDefeat()
        {
            if(phase==ArenaPhase.Defeat)return true;
            if(phase==ArenaPhase.Victory||!TeamWiped)return false;
            phase=ArenaPhase.Defeat;countdown=DefeatRestartSeconds;
            notice="DERROTA · ambos jugadores han caído · reinicio automático";
            foreach(var hero in heroes){hero.moveSpeed=0;if(hero.agent&&hero.agent.enabled&&hero.agent.isOnNavMesh)hero.agent.isStopped=true;}
            foreach(var enemy in enemies)if(enemy){enemy.preparing=false;enemy.windupRemaining=enemy.moveSpeed=0;if(enemy.agent&&enemy.agent.enabled&&enemy.agent.isOnNavMesh)enemy.agent.isStopped=true;}
            foreach(var shot in projectiles.ToArray())if(shot)Destroy(shot.gameObject);projectiles.Clear();
            return true;
        }
        public const int RoundStars=40;
        public int LastRoundStars {get;private set;}
        void CreateBonusStarPool()
        {
            var template=pickups.FirstOrDefault(p=>p.kind==SupplyKind.Star);if(!template)return;
            var list=pickups.ToList();int count=Mathf.Min(RoundStars,192-list.Count);
            var holder=new GameObject("Estrellas extra entre hordas").transform;holder.SetParent(transform,false);
            for(int i=0;i<count;i++)
            {
                var clone=Instantiate(template.gameObject,new Vector3(0,-30-i,0),Quaternion.identity,holder);
                clone.name="Estrella extra "+i;var star=clone.GetComponent<SurvivalPickup>();star.arena=this;star.id=list.Count;star.bonusStar=true;star.available=false;star.collector=0;star.rechargeEachRest=star.reserveDrop=false;list.Add(star);
            }
            pickups=list.ToArray();
        }
        public int ReplenishRoundStars()
        {
            int added=0;var random=new System.Random(7919*Mathf.Max(1,wave));
            foreach(var star in pickups.Where(p=>p.kind==SupplyKind.Star&&!p.available).OrderByDescending(p=>p.bonusStar).ThenBy(p=>SupplyOrder(p,wave)))
            {
                if(added>=RoundStars)break;
                bool placed=false;Vector3 point=Vector3.zero;
                for(int attempt=0;attempt<40&&!placed;attempt++)
                {
                    int zone=added%5;
                    float x,z,y;
                    if(zone==3){x=-28+(float)random.NextDouble()*6;z=-32+(float)random.NextDouble()*14;y=2.8f;}
                    else if(zone==4&&added%10!=9){x=22+(float)random.NextDouble()*6;z=-20+(float)random.NextDouble()*15;y=2.8f;}
                    else if(added%10==9){x=-26+(float)random.NextDouble()*52;z=14+(float)random.NextDouble()*4;y=4;}
                    else{x=-27+(float)random.NextDouble()*54;z=-51+(float)random.NextDouble()*61;y=0;}
                    if(!NavMesh.SamplePosition(new Vector3(x,y,z),out var hit,.8f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-y)>.35f)continue;
                    point=hit.position;
                    if(pickups.Any(p=>p.available&&(p.transform.position-point).sqrMagnitude<1.25f)||heroes.Any(h=>(h.transform.position-point).sqrMagnitude<4.8f))continue;
                    placed=true;
                }
                if(!placed)continue;
                star.transform.position=point;star.Recharge();added++;
            }
            LastRoundStars=added;return added;
        }

        void Awake()
        {
            Instance=this;
            if(navigation)navInstance=NavMesh.AddNavMeshData(navigation);
            foreach(var h in heroes)h.arena=this;
            foreach(var p in pickups)p.arena=this;
            CreateBonusStarPool();countdown=restSeconds;
        }
        void Start(){foreach(var h in heroes)h.Initialize();input=new SurvivalInputReader(this);RefreshSuppliesForRound(SupplyRound);}
        void OnDestroy(){input?.Dispose();if(navInstance.valid)navInstance.Remove();if(Instance==this)Instance=null;}
        public void SwitchLocalHero()
        {
            if(phase==ArenaPhase.Defeat||SurvivalSession.Instance&&SurvivalSession.Instance.Connected)return;
            var old=commands[activeHero];old.Move=Vector2.zero;old.Attack=false;old.Guard=false;commands[activeHero]=old;activeHero=1-activeHero;
        }
        void Update()
        {
            bool connected=SurvivalSession.Instance&&SurvivalSession.Instance.Connected;
            if(connected)activeHero=SurvivalSession.Instance.IsHost?0:1;
            if(input!=null)commands[activeHero]=input.Read(commands[activeHero],sharedCamera.GetComponent<Camera>());
            if(!Running)
            {
                foreach(var h in heroes)if(h.agent&&h.agent.enabled&&h.agent.isOnNavMesh)h.agent.isStopped=true;
                foreach(var e in enemies)if(e&&e.agent&&e.agent.enabled&&e.agent.isOnNavMesh)e.agent.isStopped=true;
            }
        }
        void FixedUpdate(){if(!externalSimulation&&Authoritative&&Running)Tick(Time.fixedDeltaTime);}
        public void Tick(float dt)
        {
            // Latch a team wipe before processing T, so spare lives cannot undo the defeat.
            if(phase==ArenaPhase.Defeat)
            {countdown=Mathf.Max(0,countdown-dt);if(countdown<=0)RestartArena();return;}
            if(CheckDefeat())return;
            foreach(var h in heroes)h.Tick(commands[h.id],dt);
            if(CheckDefeat()||phase==ArenaPhase.Victory)return;
            if(phase==ArenaPhase.Preparation||phase==ArenaPhase.Rest)
            {
                countdown=Mathf.Max(0,countdown-dt);
                if(countdown<=0)BeginNextWave();
            }
            else if(phase==ArenaPhase.Wave)
            {
                nextSpawn-=dt;
                bool finalBoss=wave==5&&pending==1;
                if(pending>0&&LivingEnemies<maxSimultaneous&&nextSpawn<=0&&(!finalBoss||LivingEnemies==0))
                {
                    if(TrySpawn(finalBoss?FoeKind.Giant:Composition())){pending--;nextSpawn=spawnInterval;}
                    else nextSpawn=.3f;
                }
                foreach(var enemy in enemies.ToArray())
                {if(enemy&&enemy.health>0)enemy.Tick(dt);if(phase==ArenaPhase.Defeat)break;}
                if(CheckDefeat())return;
                if(pending==0&&LivingEnemies==0)FinishWave();
            }
            foreach(var shot in projectiles.ToArray())
            {if(shot)shot.Tick(dt);if(phase==ArenaPhase.Defeat)break;}
            if(CheckDefeat())return;
            pickupClock-=dt;
            if(pickupClock<=0)
            {
                pickupClock=.09f;
                foreach(var p in pickups)if(p.available)
                    foreach(var h in heroes)if(!h.Down&&Mathf.Abs(h.transform.position.y-p.transform.position.y)<1.2f&&Vector3.Distance(h.transform.position,p.transform.position)<1.65f)
                        if(p.TryCollect(h))break;
            }
        }
        public void BeginNextWave()
        {
            if(wave>=5)return;
            wave++;pending=waveSizes[wave-1];nextSpawn=.6f;phase=ArenaPhase.Wave;
            notice="Horda "+wave+" / 5 · vigila las entradas de todos los lados";
        }
        FoeKind Composition()
        {
            int ordinal=waveSizes[wave-1]-pending;
            if(wave>=3&&ordinal%4==3)return FoeKind.Bomber;
            if(wave>=2&&ordinal%3==1)return FoeKind.Dagger;
            if(wave>=3&&ordinal%3==2)return FoeKind.Runner;
            if(ordinal%2==0)return FoeKind.Goblin;
            return FoeKind.Soldier;
        }
        public bool TrySpawn(FoeKind kind)
        {
            for(int i=0;i<entrances.Length;i++)
            {
                var p=entrances[(entranceIndex+i)%entrances.Length];
                if(heroes.Any(h=>!h.Down&&Vector3.Distance(h.transform.position,p)<7))continue;
                if(!NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas))continue;
                entranceIndex=(entranceIndex+i+1)%entrances.Length;
                SpawnEnemy(kind,hit.position);return true;
            }
            return false;
        }
        public SurvivalEnemy SpawnEnemy(FoeKind kind,Vector3 at)
        {
            var go=new GameObject(kind==FoeKind.Giant?"Gigante de la maza":"Invasor · "+kind);
            go.transform.position=at;
            var foe=go.AddComponent<SurvivalEnemy>();foe.arena=this;foe.kind=kind;
            foe.id=enemies.Count;foe.Initialize(wave);enemies.Add(foe);
            if(kind==FoeKind.Giant)notice="¡GIGANTE CON MAZA! Sal del círculo antes del impacto";
            return foe;
        }
        public void FinishWave()
        {
            foreach(var shot in projectiles.ToArray())if(shot)Destroy(shot.gameObject);projectiles.Clear();
            if(wave==5)
            {phase=ArenaPhase.Victory;notice="PLAZA DEFENDIDA · cinco hordas superadas · salida abierta";if(exitGate)exitGate.gameObject.SetActive(false);}
            else
            {
                phase=ArenaPhase.Rest;countdown=restSeconds;
                RefreshSuppliesForRound(SupplyRound);ReplenishRoundStars();
                notice="Descanso · +"+LastRoundStars+" estrellas · horda "+SupplyRound+" · curas +"+HealthRestore+" HP";
            }
        }
        public void EnemyDefeated(SurvivalEnemy foe){totalKills++;}
        static uint SupplyOrder(SurvivalPickup p,int round)
        {
            unchecked{uint n=(uint)p.id^((uint)round*2654435761u);n^=n>>16;n*=0x7feb352du;n^=n>>15;n*=0x846ca68bu;return n^(n>>16);}
        }
        public void RefreshSuppliesForRound(int round)
        {
            // Clear legacy enemy-drop slots and reduce all uncollected stations as well as refills.
            foreach(var p in pickups)if(p.reserveDrop)p.available=false;
            foreach(SupplyKind kind in Enum.GetValues(typeof(SupplyKind)))
            {
                if(kind==SupplyKind.Star)continue;
                var stations=pickups.Where(p=>p.rechargeEachRest&&!p.reserveDrop&&p.kind==kind).OrderBy(p=>SupplyOrder(p,round)).ToArray();
                int budget=SurvivalSupplyRules.Budget(kind,round);
                for(int i=0;i<stations.Length;i++){stations[i].collector=0;stations[i].available=i<budget;}
            }
        }
        public bool CollectHealth(SurvivalHero collector)
        {
            if(!collector||collector.Down||collector.health>=100)return false;
            int restored=Mathf.Min(HealthRestore,100-collector.health);collector.health+=restored;
            notice=(collector.IsMage?"Mago":"Guerrero")+" · +"+restored+" de salud";return true;
        }
        public Vector3 SafePoint(Vector3 at)
        {
            if(NavMesh.SamplePosition(at,out var hit,4,NavMesh.AllAreas))return hit.position;
            return new Vector3(0,0,-10);
        }
        public void RestartArena()
        {
            foreach(var e in enemies)if(e)Destroy(e.gameObject);enemies.Clear();
            foreach(var p in projectiles)if(p)Destroy(p.gameObject);projectiles.Clear();
            wave=pending=totalKills=0;LastRoundStars=0;entranceIndex=0;nextSpawn=pickupClock=0;phase=ArenaPhase.Preparation;countdown=restSeconds;
            for(int i=0;i<commands.Length;i++){var cmd=commands[i];cmd.Move=Vector2.zero;cmd.Attack=cmd.Guard=false;commands[i]=cmd;}
            foreach(var h in heroes)h.ResetHero();foreach(var p in pickups)p.ResetForMatch();RefreshSuppliesForRound(SupplyRound);
            if(exitGate)exitGate.gameObject.SetActive(true);
            notice="Nueva defensa · busca equipo en la plaza y las galerías";
        }
        public bool ClearLine(Vector3 from,Vector3 to)
        {
            var d=to-from;
            foreach(var hit in Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.collider.GetComponentInParent<SurvivalHero>()&&!hit.collider.GetComponentInParent<SurvivalEnemy>())return false;
            return true;
        }
    }
}

