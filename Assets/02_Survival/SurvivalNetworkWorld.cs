using System.Linq;
using Fusion;
using UnityEngine;

namespace Kingdoms.Survival
{
    public struct ArenaHeroState : INetworkStruct
    {
        public Vector3 Position,Heading;
        public int Health,Armor,Stars,Lives,Attack,Spell;
        public float Fire,Ice,Protection,Recovery,AttackAge,Speed;
        public NetworkBool Equipped,Guarding,Protected,Airborne;
    }
    public struct ArenaEnemyState : INetworkStruct
    {
        public Vector3 Position,Heading;
        public int Kind,Health,Shield,Attack;
        public float Speed,Windup,Frozen;
        public NetworkBool Active,Preparing;
    }
    public struct ArenaPickupState : INetworkStruct { public Vector3 Position;public int Collector;public NetworkBool Available; }
    public struct ArenaShotState : INetworkStruct { public Vector3 Position,Velocity;public int Owner,Damage;public float Fuse;public NetworkBool Active,Ice,Dagger,Fire,Bomb; }

    // Same host-authoritative snapshot model as Pueblo, with full X/Y/Z positions.
    public sealed class SurvivalNetworkWorld : NetworkBehaviour
    {
        [Networked] public PlayerRef Warrior {get;set;}
        [Networked,Capacity(2)] public NetworkArray<ArenaHeroState> Heroes=>default;
        [Networked,Capacity(64)] public NetworkArray<ArenaEnemyState> Enemies=>default;
        [Networked,Capacity(192)] public NetworkArray<ArenaPickupState> Pickups=>default;
        [Networked,Capacity(48)] public NetworkArray<ArenaShotState> Shots=>default;
        [Networked] public int Phase {get;set;}
        [Networked] public int Wave {get;set;}
        [Networked] public int Pending {get;set;}
        [Networked] public int EnemyCount {get;set;}
        [Networked] public float Countdown {get;set;}
        [Networked] public NetworkString<_256> Notice {get;set;}
        SurvivalArena arena;
        readonly SurvivalProjectile[] shots=new SurvivalProjectile[48];
        public override void Spawned()
        {
            arena=SurvivalArena.Instance;arena.externalSimulation=true;
            foreach(var hero in arena.heroes)hero.Initialize();
            Debug.Log("SURVIVAL WORLD SPAWNED authority="+HasStateAuthority+" heroes="+arena.heroes.Length);
            if(HasStateAuthority)Warrior=Runner.LocalPlayer;
            else foreach(var hero in arena.heroes)hero.agent.enabled=false;
        }
        public override void FixedUpdateNetwork()
        {
            if(!HasStateAuthority)return;
            var players=Runner.ActivePlayers.ToArray();
            if(players.Length==2)
            {
                Runner.TryGetInputForPlayer(Warrior,out SurvivalCommand first);
                Runner.TryGetInputForPlayer(players.First(p=>p!=Warrior),out SurvivalCommand second);
                arena.commands[0]=first;arena.commands[1]=second;arena.Tick(Runner.DeltaTime);
            }
            for(int i=0;i<2;i++)
            {
                var h=arena.heroes[i];Heroes.Set(i,new ArenaHeroState{Position=h.transform.position,Heading=h.heading,Health=h.health,Armor=h.armor,Stars=h.stars,Lives=h.lives,Attack=h.attackSerial,Spell=h.selectedSpell,Fire=h.fire,Ice=h.ice,Protection=h.protection,Recovery=Mathf.Max(0,h.immuneUntil-Time.time),AttackAge=h.attackAge,Speed=h.moveSpeed,Equipped=h.equipped,Guarding=h.guarding,Protected=h.protectedByMagic,Airborne=h.airborne});
            }
            EnemyCount=Mathf.Min(64,arena.enemies.Count);
            for(int i=0;i<EnemyCount;i++)
            {var e=arena.enemies[i];Enemies.Set(i,new ArenaEnemyState{Position=e.transform.position,Heading=e.heading,Kind=(int)e.kind,Health=e.health,Shield=e.shieldHealth,Attack=e.attackSerial,Speed=e.moveSpeed,Windup=e.windupRemaining,Preparing=e.preparing,Active=e.gameObject.activeSelf,Frozen=e.FrozenRemaining});}
            for(int i=0;i<arena.pickups.Length;i++){var p=arena.pickups[i];Pickups.Set(i,new ArenaPickupState{Position=p.transform.position,Collector=p.collector,Available=p.available});}
            for(int i=0;i<48;i++)
            {var p=i<arena.projectiles.Count?arena.projectiles[i]:null;Shots.Set(i,p?new ArenaShotState{Active=true,Position=p.transform.position,Velocity=p.velocity,Owner=p.owner,Damage=p.damage,Ice=p.ice,Dagger=p.dagger,Fire=p.fire,Bomb=p.bomb,Fuse=p.bombFuse}:default);}
            Phase=(int)arena.phase;Wave=arena.wave;Pending=arena.pending;Countdown=arena.countdown;Notice=arena.notice;
        }
        public override void Render()
        {
            if(!arena||HasStateAuthority)return;
            arena.phase=(ArenaPhase)Phase;arena.wave=Wave;arena.pending=Pending;arena.countdown=Countdown;arena.notice=Notice.ToString();
            if(arena.exitGate)arena.exitGate.gameObject.SetActive(arena.phase!=ArenaPhase.Victory);
            for(int i=0;i<2;i++)
            {
                var s=Heroes[i];var h=arena.heroes[i];h.transform.position=Vector3.Distance(h.transform.position,s.Position)>4?s.Position:Vector3.Lerp(h.transform.position,s.Position,1-Mathf.Exp(-25*Time.deltaTime));h.heading=s.Heading;h.health=s.Health;h.armor=s.Armor;h.stars=s.Stars;h.lives=s.Lives;h.attackSerial=s.Attack;h.selectedSpell=s.Spell;h.fire=s.Fire;h.ice=s.Ice;h.protection=s.Protection;h.immuneUntil=Time.time+s.Recovery;h.attackAge=s.AttackAge;h.moveSpeed=s.Speed;h.equipped=s.Equipped;h.guarding=s.Guarding;h.protectedByMagic=s.Protected;h.airborne=s.Airborne;
            }
            while(arena.enemies.Count>EnemyCount){var last=arena.enemies[arena.enemies.Count-1];arena.enemies.RemoveAt(arena.enemies.Count-1);if(last)Destroy(last.gameObject);}
            while(arena.enemies.Count<EnemyCount)
            {var s=Enemies[arena.enemies.Count];var e=arena.SpawnEnemy((FoeKind)s.Kind,s.Position);e.agent.enabled=false;}
            for(int i=0;i<EnemyCount;i++)
            {var s=Enemies[i];var e=arena.enemies[i];e.gameObject.SetActive(s.Active);e.transform.position=Vector3.Lerp(e.transform.position,s.Position,1-Mathf.Exp(-25*Time.deltaTime));e.heading=s.Heading;e.health=s.Health;e.shieldHealth=s.Shield;e.attackSerial=s.Attack;e.moveSpeed=s.Speed;e.preparing=s.Preparing;e.windupRemaining=s.Windup;e.ApplyRemoteFreeze(s.Frozen);}
            for(int i=0;i<arena.pickups.Length;i++){var s=Pickups[i];var p=arena.pickups[i];p.transform.position=s.Position;p.collector=s.Collector;p.available=s.Available;}
            for(int i=0;i<48;i++)
            {
                var s=Shots[i];if(!s.Active){if(shots[i])Destroy(shots[i].gameObject);shots[i]=null;continue;}
                if(shots[i]&&(shots[i].bomb!=s.Bomb||shots[i].dagger!=s.Dagger||shots[i].ice!=s.Ice||shots[i].fire!=s.Fire||shots[i].owner!=s.Owner)){Destroy(shots[i].gameObject);shots[i]=null;}
                if(!shots[i])shots[i]=SurvivalProjectile.Create(arena,s.Position,s.Velocity,s.Owner,s.Damage,s.Ice,s.Dagger,s.Fire,s.Bomb);
                shots[i].transform.position=s.Position;shots[i].velocity=s.Velocity;shots[i].bombFuse=s.Fuse;
            }
        }
    }
}


