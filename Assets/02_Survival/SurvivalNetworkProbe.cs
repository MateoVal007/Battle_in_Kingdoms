using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kingdoms.Survival
{
    // Opt-in verification for two separate player processes; inactive in normal play.
    public sealed class SurvivalNetworkProbe : MonoBehaviour
    {
        public static SurvivalNetworkProbe Instance;
        float joined=-1,started;
        bool scenario,done,sawMovement,sawAir,sawDown,sawFrozen,sawThaw,balanced,baselinePassed,wipeStarted,sawTeamDefeat;
        string output;
        int exitCode;Vector3 deathPoint;bool sawOwnRevive;
        bool sawBomber,sawBomb,sawBlast,sawRetreat,sawBrokenFreeze;
        bool armorPrepared,armorBroken,sawArmorBreak,initialEquip,edgePlaced,sawUpper,sawEdgeFall,sawEdgeLanding,sawGoblin,sawJog;
        public static string Arg(string key){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OpenRequestedArena()
        {
            if((Arg("-survival-role")!=null||Arg("-survival-play")!=null)&&SceneManager.GetActiveScene().name!="Supervivencia_02")
            {
                if(VillageOnline.Instance)Destroy(VillageOnline.Instance.gameObject);
                SceneManager.LoadScene("Supervivencia_02");
            }
        }
        void Awake(){Instance=this;started=Time.time;output=Arg("-survival-report");}
        public SurvivalCommand Input(bool host)
        {
            float age=joined<0?-1:Time.time-joined;
            return new SurvivalCommand{ClickSerial=1,Destination=new Vector3(host?-8:8,0,-7),Aim=Vector3.zero,HasAim=true,JumpSerial=age>1?1:0,LifeSerial=age>14.2f?(host?1:2):!host&&age>5?1:0,UseSerial=age>14.2f?1:0,Move=age>10.5f&&age<12.4f?(host?Vector2.right:Vector2.left):Vector2.zero};
        }

        void Update()
        {
            if(done)return;
            var session=SurvivalSession.Instance;var arena=SurvivalArena.Instance;
            if(Time.time-started>80){Finish(false,"Timeout: "+(session?session.status:"no session")+" external="+(arena&&arena.externalSimulation)+" positions="+(arena?string.Join(";",arena.heroes.Select(h=>h.transform.position.ToString())):"none")+" wave="+(arena?arena.wave:0));return;}
            if(!session||!session.Connected||session.PlayerCount!=2||!arena||!arena.externalSimulation)return;
            if(joined<0)joined=Time.time;
            var warrior=arena.heroes[0];
            if(session.IsHost&&!initialEquip&&Time.time-joined>.1f){initialEquip=true;warrior.equipped=true;warrior.armor=60;}
            if(session.IsHost&&!edgePlaced&&Time.time-joined>10.1f){edgePlaced=true;arena.heroes[0].Revive(new Vector3(-23,2.8f,-25),100);arena.heroes[1].Revive(new Vector3(23,2.8f,-12),100);
                arena.SpawnEnemy(FoeKind.Bomber,new Vector3(-19,0,-25));
                var broken=arena.enemies.First(e=>e.kind==FoeKind.Dagger);broken.Hit(40,true,broken.transform.position+broken.heading*3,true);broken.Hit(1,true,broken.transform.position+broken.heading*3,true);
            }
            if(Time.time-joined>10){sawUpper|=arena.heroes.All(h=>h.transform.position.y>2.6f);sawEdgeFall|=sawUpper&&arena.heroes.Any(h=>h.airborne&&h.transform.position.y<2.6f&&h.transform.position.y>.2f);sawEdgeLanding|=sawEdgeFall&&arena.heroes.All(h=>h.transform.position.y<.15f&&!h.airborne);}
            var bomber=arena.enemies.FirstOrDefault(e=>e&&e.kind==FoeKind.Bomber);
            if(bomber){if(session.IsHost)bomber.Tick(Time.deltaTime);sawBomber|=bomber.GetComponentsInChildren<Animator>().Any(a=>a.runtimeAnimatorController&&a.runtimeAnimatorController.name=="Bomber");sawRetreat|=bomber.transform.position.x> -18.4f;}
            sawBomb|=arena.projectiles.Any(p=>p&&p.bomb&&p.bombFuse>0);sawBlast|=arena.projectiles.Any(p=>p&&p.bomb&&p.bombFuse<=0);
            sawBrokenFreeze|=arena.enemies.Any(e=>e&&e.kind==FoeKind.Dagger&&e.shieldHealth==0&&e.IsFrozen);
            sawGoblin|=arena.enemies.Any(e=>e&&e.kind==FoeKind.Goblin&&e.GetComponentsInChildren<Animator>().Any(a=>a.runtimeAnimatorController&&a.runtimeAnimatorController.name=="Goblin"));
            if(session.IsHost&&!armorPrepared&&Time.time-joined>8.1f){armorPrepared=true;warrior.equipped=true;warrior.armor=1;}
            if(session.IsHost&&armorPrepared&&!armorBroken&&Time.time-joined>8.6f){armorBroken=true;warrior.guarding=true;warrior.immuneUntil=0;warrior.Damage(20,warrior.transform.position+warrior.heading*2);}
            var warriorVisual=warrior.GetComponentInChildren<VillageWarriorVisual>();
            sawArmorBreak|=warriorVisual&&warriorVisual.ArmorBreakPlaying;
            sawJog|=warriorVisual&&warriorVisual.libraryAttack.gameObject.activeSelf&&warriorVisual.libraryAttack.GetCurrentAnimatorStateInfo(0).IsName("JogEquipped");
            bool moved=arena.heroes[0].transform.position.x< -7&&arena.heroes[1].transform.position.x>7;
            var frozenFoe=arena.enemies.FirstOrDefault(e=>e.kind==FoeKind.Soldier);
            if(frozenFoe){sawFrozen|=frozenFoe.IsFrozen;sawThaw|=sawFrozen&&!frozenFoe.IsFrozen;}
            if(arena.heroes[1].Down&&arena.heroes[1].lives==4){deathPoint=arena.heroes[1].transform.position;sawDown=true;}
            if(sawDown&&!arena.heroes[1].Down)sawOwnRevive|=arena.heroes[1].health==100&&Vector3.Distance(arena.heroes[1].transform.position,deathPoint)<.25f;
            sawMovement|=moved;sawAir|=arena.heroes.Any(h=>h.airborne&&h.transform.position.y>1);
            if(session.IsHost&&!scenario&&Time.time-joined>3&&moved)
            {
                scenario=true;arena.SpawnEnemy(FoeKind.Goblin,new Vector3(0,0,-1));
                var mage=arena.heroes[1];mage.stars=99;mage.lives=3;
                arena.pickups.First(p=>p.kind==SupplyKind.Star&&p.available).TryCollect(mage);
                mage.immuneUntil=0;mage.Damage(9999,mage.transform.position);if(arena.CollectHealth(arena.heroes[0])||!mage.Down){Finish(false,"Health pickup revived a teammate or healed a full collector");return;}
                var foe=arena.SpawnEnemy(FoeKind.Dagger,new Vector3(0,0,-1));foe.heading=Vector3.right;foe.Hit(20,false,Vector3.right*3+foe.transform.position,true);
                var frozen=arena.SpawnEnemy(FoeKind.Soldier,new Vector3(0,0,-4));frozen.Hit(5,true,frozen.transform.position+Vector3.back*3,false);
                if(!frozen.IsFrozen||foe.IsFrozen){Finish(false,"Wrong freeze immunity");return;}
                arena.countdown=13;
            }
            if(session.IsHost&&scenario&&!balanced&&Time.time-joined>7)
            {balanced=true;arena.wave=4;arena.FinishWave();arena.countdown=13;}
            sawTeamDefeat|=arena.phase==ArenaPhase.Defeat&&arena.heroes.All(h=>h.Down&&h.lives==7);
            if(sawBomber&&sawBomb&&sawBlast&&sawRetreat&&sawBrokenFreeze&&sawJog&&sawGoblin&&sawEdgeLanding&&sawArmorBreak&&sawTeamDefeat&&arena.phase==ArenaPhase.Preparation&&arena.wave==0&&arena.heroes.All(h=>h.health==100&&h.lives==3&&h.fire==12)&&arena.enemies.Count==0&&arena.pickups.Count(p=>p.kind==SupplyKind.Star&&p.available)<=108)
            {Finish(true,"Two processes: Bomber retreat, bomb flight/explosion and freeze after shield destruction replicated; equipped Jog_Fwd and larger Goblin retained; both heroes walk off upper galleries without jumping and land physically; armor break, individual T revive, shared pickups and automatic team retry remain synchronized.");return;}
            if(Time.time-joined>8)
            {
                var mage=arena.heroes[1];var foe=arena.enemies.FirstOrDefault(e=>e.kind==FoeKind.Dagger);
                bool valid=sawMovement&&sawAir&&sawDown&&sawOwnRevive&&mage.health==100&&mage.lives==3&&arena.pickups.Any(p=>p.kind==SupplyKind.Star&&!p.available&&p.collector==2)&&foe&&foe.shieldHealth==40&&foe.health==70&&arena.countdown<13&&sawFrozen&&sawThaw&&arena.phase==ArenaPhase.Rest&&arena.SupplyRound==5&&arena.HealthRestore==15&&arena.pickups.Count(p=>p.rechargeEachRest&&p.available)==12&&arena.pickups.Count(p=>p.bonusStar&&p.available)==40;
                if(!valid&&!string.IsNullOrEmpty(output))File.WriteAllText(output+".progress","bomber="+sawBomber+" bomb="+sawBomb+" blast="+sawBlast+" retreat="+sawRetreat+" brokenFreeze="+sawBrokenFreeze+" host="+session.IsHost+" scenario="+scenario+" moved="+sawMovement+" sawAir="+sawAir+" sawDown="+sawDown+" frozen="+sawFrozen+" thaw="+sawThaw+" supplyRound="+arena.SupplyRound+" supplies="+arena.pickups.Count(p=>p.rechargeEachRest&&p.available)+" ownRevive="+sawOwnRevive+" bonus="+arena.pickups.Count(p=>p.bonusStar&&p.available)+" mage hp="+mage.health+" lives="+mage.lives+" stars="+mage.stars+" shield="+(foe?foe.shieldHealth:-1)+" foeHp="+(foe?foe.health:-1)+" countdown="+arena.countdown);
                baselinePassed|=valid;
                if(session.IsHost&&baselinePassed&&sawEdgeLanding&&sawBomb&&sawBlast&&sawRetreat&&sawBrokenFreeze&&!wipeStarted&&Time.time-joined>13.5f)
                {
                    wipeStarted=true;foreach(var h in arena.heroes){h.lives=7;h.immuneUntil=0;h.protectedByMagic=h.guarding=false;h.Damage(9999,h.transform.position);}
                }
            }
        }
        void Finish(bool passed,string message)
        {done=true;exitCode=passed?0:1;if(!string.IsNullOrEmpty(output))File.WriteAllText(output,(passed?"PASS ":"FAIL ")+message);Invoke(nameof(ExitProbe),3);}
        void ExitProbe(){Application.Quit(exitCode);}
    }
}
