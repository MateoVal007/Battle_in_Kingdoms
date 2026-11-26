using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct VillageHeroState : INetworkStruct
{
    public Vector3 Position;
    public int Health,Armor,Attack,Stars,Lives;
    public float Facing,Recovery,MoveSpeed;
    public NetworkBool Blocking,Casting;
    public NetworkBool Crouching,ShieldUp,ItemActive,HasEquipment,Airborne;
    public int SelectedItem;
    public float FireCharge,IceCharge,ProtectionCharge;
    public NetworkString<_32> Enchantment;
}
public struct VillageEntityState : INetworkStruct {public Vector3 Position;public NetworkBool Active,Opened,Loot;public int Health,ShieldHealth;public Color Color;public float Facing,Action,Frozen,Fuse;public Vector3 Velocity;public NetworkBool Arrow,Dagger,Bomb;}

// Initial host-authoritative prototype. Clients render snapshots, without local prediction.
public class VillageNetworkWorld : NetworkBehaviour
{
    [Networked] public PlayerRef WarriorPlayer {get;set;}
    [Networked] public PlayerRef MagePlayer {get;set;}
    [Networked,Capacity(2)] public NetworkArray<VillageHeroState> Heroes => default;
    [Networked,Capacity(64)] public NetworkArray<VillageEntityState> Entities => default;
    [Networked,Capacity(32)] public NetworkArray<VillageEntityState> Projectiles => default;
    [Networked,Capacity(VillageStar.Capacity)] public NetworkArray<int> StarMasks=>default;
    [Networked] public NetworkBool GateOpen {get;set;}
    [Networked] public NetworkBool Completed {get;set;}
    [Networked] public float Charge {get;set;}
    [Networked] public int LiftRequester {get;set;}
    [Networked] public float LiftRequestLeft {get;set;}
    [Networked] public float LiftCarryLeft {get;set;}
    [Networked] public float Checkpoint {get;set;}
    [Networked] public NetworkString<_256> Notice {get;set;}
    [Networked] public int TestPhase {get;set;}
    [Networked] public int TestFlags {get;set;}
    public VillageGame Game {get;private set;}
    VillageEnemy[] enemies;VillageChest[] chests;VillageZone[] zones;VillageStar[] stars;
    readonly int[] lastLiftPress = new int[2];
    readonly NetworkButtons[] previous=new NetworkButtons[2];
    readonly GameObject[] visuals=new GameObject[32];
    float phaseAt;bool reported;
    bool sawLiftRequest, sawLiftCarry, sawLiftRelease, liftGateSequence;
    bool sawLiftTimeout, sawLiftReject;
    const int DaggerCheck = 1 << 30;
    const int ArmorCheck = 1 << 31;
    bool sawDagger, sawBrokenEnemyShield, daggerSequence;
    VillageEnemy testDagger;
    VillageChest testSecret;
    int secretsRecovered;
    float highestUnassisted;
    bool sawRemoteMovement,sawChest,sawDown,sawRevive;
    bool sawJump;
    bool sawPersonalStars,sawSharedStars,sawExtraLife,sawRecovery,sawProtectionField,sawMageCrouch;int crouchAttack;
    bool sawEnemyChase,sawArrow,sawWhip,sawEnemyDeath,sawGearBreak,whipWarned;
    int archerAttacks;string pursuitCheck;
    bool sawCrouch,sawUpperShield,sawItemUse,sawItemSwitch,sawEquipment;
    static T[] Ordered<T>() where T:Component => FindObjectsByType<T>(FindObjectsInactive.Include).OrderBy(x=>x.transform.position.x).ThenBy(x=>x.transform.position.y).ThenBy(x=>x.name,StringComparer.Ordinal).ToArray();
    public override void Spawned()
    {
        Game=FindFirstObjectByType<VillageGame>();VillageOnline.Instance.world=this;
        enemies=Ordered<VillageEnemy>();chests=Ordered<VillageChest>();zones=Ordered<VillageZone>().Where(z=>!z.spawnedByChest).ToArray();
        stars=FindObjectsByType<VillageStar>(FindObjectsInactive.Include).OrderBy(s=>s.id).ToArray();
        if(stars.Length>VillageStar.Capacity||stars.Any(s=>s.id<0||s.id>=VillageStar.Capacity)||stars.Select(s=>s.id).Distinct().Count()!=stars.Length)throw new InvalidOperationException("Invalid star registry");
        if(enemies.Length+chests.Length+zones.Length>64)throw new InvalidOperationException("World snapshot capacity exceeded");
        if(HasStateAuthority)WarriorPlayer=Runner.LocalPlayer;
        else {Game.warrior.GetComponent<CharacterController>().enabled=false;Game.mage.GetComponent<CharacterController>().enabled=false;}
    }
    public override void FixedUpdateNetwork()
    {
        if(!HasStateAuthority)return;
        var players=Runner.ActivePlayers.ToArray();MagePlayer=players.FirstOrDefault(p=>p!=WarriorPlayer);
        if(players.Length==2)
        {
            var warriorInput = ReadInput(WarriorPlayer, 0, out var warriorPressed);
            var mageInput = ReadInput(MagePlayer, 1, out var magePressed);
            Game.Lift.Tick(Runner.DeltaTime, NewLiftPress(warriorInput.LiftPress, 0), NewLiftPress(mageInput.LiftPress, 1),
                new Vector2(mageInput.Move, mageInput.LiftY));
            Step(Game.warrior, warriorInput, warriorPressed);
            Step(Game.mage, mageInput, magePressed);
            if(VillageOnline.Instance.Smoke)TestStep();
        }
        LiftRequester = Game.Lift.Requester;
        LiftRequestLeft = Game.Lift.RequestLeft;
        LiftCarryLeft = Game.Lift.CarryLeft;
        foreach(var star in stars)StarMasks.Set(star.id,star.mask);
        Heroes.Set(0,Capture(Game.warrior));Heroes.Set(1,Capture(Game.mage));
        GateOpen=Game.gateOpen;Charge=Game.runeCharge;Completed=Game.completed;Checkpoint=Game.checkpoint;Notice=Game.notice;
        int index=0;
        foreach(var e in enemies)Entities.Set(index++,new VillageEntityState{Active=e&&e.gameObject.activeSelf,Position=e?e.transform.position:Vector3.zero,Health=e?e.health:0,ShieldHealth=e?e.shieldHealth:0,Facing=e?e.Facing:0,Action=e?e.AttackVisual:0,Frozen=e?e.FrozenRemaining:0});
        foreach(var c in chests)Entities.Set(index++,new VillageEntityState{Opened=c.opened,Loot=c.LootActive});
        foreach(var z in zones)Entities.Set(index++,new VillageEntityState{Active=z.gameObject.activeSelf});
        var shots=FindObjectsByType<VillageBolt>().Select(b=>b.gameObject).Concat(FindObjectsByType<VillageEnemyBolt>().Select(b=>b.gameObject)).Take(32).ToArray();
        for(int i=0;i<32;i++)
        {
            if(i>=shots.Length){Projectiles.Set(i,default);continue;}
            var bolt=shots[i].GetComponent<VillageEnemyBolt>();
            Projectiles.Set(i,new VillageEntityState{Active=true,Position=shots[i].transform.position,Color=shots[i].GetComponent<Renderer>().material.color,Arrow=bolt&&bolt.arrow,Dagger=bolt&&bolt.dagger,Bomb=bolt&&bolt.bomb,Fuse=bolt?bolt.bombFuse:0,Velocity=bolt?bolt.velocity:Vector3.zero});
        }
    }
    bool NewLiftPress(int press, int index)
    {
        if (press == 0 || press == lastLiftPress[index]) return false;
        lastLiftPress[index] = press;
        return true;
    }

    VillageInput ReadInput(PlayerRef player, int index, out NetworkButtons pressed)
    {
        if (!Runner.TryGetInputForPlayer(player, out VillageInput input))
        {
            pressed = default;
            return default;
        }
        pressed = input.Buttons.GetPressed(previous[index]);
        previous[index] = input.Buttons;
        return input;
    }

    void Step(VillageHero hero, VillageInput input, NetworkButtons pressed)
    {
        hero.Simulate(Mathf.Clamp(input.Move,-1,1),pressed.IsSet(0),pressed.IsSet(1),input.Buttons.IsSet(2),Runner.DeltaTime,
            input.Buttons.IsSet(3),input.Buttons.IsSet(4),(pressed.IsSet(6)?1:0)-(pressed.IsSet(5)?1:0),pressed.IsSet(7),!hero.isMage&&input.Buttons.IsSet(9));
        if(pressed.IsSet(8))hero.TryUseExtraLife();
    }
    static VillageHeroState Capture(VillageHero h)=>new VillageHeroState{Position=h.transform.position,Health=h.health,Armor=h.armor,Stars=h.stars,Lives=h.extraLives,Recovery=h.RecoveryRemaining,Facing=h.Facing,Blocking=h.blocking,Casting=h.casting,Attack=h.AttackSerial,Enchantment=h.enchantment,
        Crouching=h.crouching,ShieldUp=h.shieldUp,SelectedItem=h.selectedItem,ItemActive=h.itemActive,FireCharge=h.fireCharge,IceCharge=h.iceCharge,ProtectionCharge=h.protectionCharge,HasEquipment=h.hasEquipment,MoveSpeed=h.PresentationSpeed,Airborne=h.PresentationAirborne};
    public override void Render()
    {
        if(!Game)return;
        if(!HasStateAuthority)
        {
            foreach(var star in stars)star.ApplyMask(StarMasks[star.id]);
            Game.warrior.ApplyRemote(Heroes[0]);Game.mage.ApplyRemote(Heroes[1]);
            Game.Lift.ApplyRemote(LiftRequester, LiftRequestLeft, LiftCarryLeft);
            Game.gateOpen=GateOpen;Game.gate.gameObject.SetActive(!GateOpen);Game.runeCharge=Charge;Game.completed=Completed;Game.checkpoint=Checkpoint;Game.notice=Notice.ToString();
            int index=0;
            foreach(var e in enemies){var s=Entities[index++];if(e){e.gameObject.SetActive(s.Active);e.transform.position=s.Position;e.health=s.Health;e.shieldHealth=s.ShieldHealth;e.ApplyRemote(s.Facing,s.Action,s.Frozen);}}
            foreach(var c in chests){var s=Entities[index++];c.ApplyRemote(s.Opened,s.Loot);}
            foreach(var z in zones)z.gameObject.SetActive(Entities[index++].Active);
            for(int i=0;i<32;i++)
            {
                var s=Projectiles[i];
                if(s.Active&&!visuals[i]){var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name="Proyectil remoto";Destroy(o.GetComponent<Collider>());o.transform.localScale=Vector3.one*.35f;visuals[i]=o;}
                if(visuals[i])
                {
                    visuals[i].SetActive(s.Active);if(!s.Active)continue;visuals[i].transform.position=s.Position;
                    var effect=visuals[i].GetComponent<EnemyBombVisual>();
                    if(s.Bomb){if(!effect)effect=visuals[i].AddComponent<EnemyBombVisual>();effect.Present(s.Fuse,false);}
                    else
                    {
                        if(effect)effect.Hide();visuals[i].GetComponent<Renderer>().material.color=s.Color;
                        if(s.Arrow||s.Dagger)VillageEnemyBolt.StyleProjectile(visuals[i].transform,s.Velocity,s.Dagger);
                        else {visuals[i].transform.localScale=Vector3.one*.35f;visuals[i].transform.rotation=Quaternion.identity;}
                    }
                }
            }
        }
        if(VillageOnline.Instance.Smoke)ObserveTest();
    }
    public VillageInput TestInput(bool host)
    {
        var input=new VillageInput();
        if(TestPhase==1){input.Move=host?-1:1;input.Buttons.Set(0,!host);}
        if(TestPhase==2&&host)input.Move=.1f;
        if(TestPhase==3)input.Buttons.Set(1,host);
        if (TestPhase == 34) input.LiftPress = 1; // Simultaneous presses cannot start a lift.
        if (TestPhase == 35 && host) input.LiftPress = 2;
        if (TestPhase == 36 && !host) input.LiftPress = 2;
        if (TestPhase == 36 && !host) input.LiftY = 1;
        if (TestPhase == 37 && !host) input.Move = 1;
        if (TestPhase == 38 && host) input.LiftPress = 3;
        if (TestPhase == 39 && !host) input.LiftPress = 3;
        if (TestPhase == 40 && host) input.LiftPress = 4;
        if (TestPhase == 42 && !host) input.LiftPress = 4;
        if (TestPhase == 43 && host) input.LiftPress = 5;
        if (TestPhase == 70)
            input.Move = Heroes[host ? 0 : 1].Position.x < 174.4f ? 1 : 0;
        if (TestPhase == 71)
        {
            input.Move = Heroes[host ? 0 : 1].Position.x < 178.2f ? 1 : 0;
            input.Buttons.Set(0, true);
        }
        if (TestPhase == 72) input.Move = 1;
        if (TestPhase >= 90 && TestPhase <= 106)
        {
            bool inner = TestPhase >= 100;
            float entry = inner ? 171.9f : 116.7f;
            int phase = TestPhase % 10;
            if (phase == 0 && host) input.Buttons.Set(0, true);
            if (phase == 1 && host) input.LiftPress = inner ? 22 : 20;
            if (phase == 2 && !host)
            {
                input.LiftPress = inner ? 22 : 20;
                input.LiftY = Heroes[0].Position.y < (inner ? 7.95f : 7f) ? 1 : 0;
            }
            if (phase == 3 && !host) input.Move = Heroes[0].Position.x < entry + 2.05f ? 1 : 0;
            if (phase == 4 && host) input.LiftPress = inner ? 23 : 21;
            if (phase == 5 && host) input.Buttons.Set(1, true);
            if (phase == 6 && host) input.Move = Heroes[0].Position.x < entry + 4.05f ? 1 : 0;
        }
        if (!host && TestPhase >= 61 && TestPhase <= 67 && TestPhase % 2 == 1)
            input.Buttons.Set(1, true);
        if(TestPhase==7){input.Buttons.Set(2,host);input.Buttons.Set(3,host);input.Buttons.Set(1,host);input.Buttons.Set(7,!host);}
        if(TestPhase==8){input.Buttons.Set(2,host);input.Buttons.Set(4,host);}
        if(TestPhase==9)input.Buttons.Set(6,!host);
        if(TestPhase==10){input.Buttons.Set(7,!host);input.Buttons.Set(1,!host);}
        if(TestPhase==11&&host&&Heroes[0].Position.x<42)input.Move=1;
        if(TestPhase==21&&host)input.Buttons.Set(2,true);
        if(TestPhase==28&&host)input.Buttons.Set(8,true);
        if(TestPhase==33&&!host){input.Buttons.Set(3,true);input.Buttons.Set(1,true);}
        return input;
    }
    void NextPhase(int phase){TestPhase=phase;phaseAt=Time.time;}
    void TestStep()
    {
        if(TestPhase==1&&Game.mage.transform.position.y>2)sawJump=true;
        float t=Time.time-phaseAt;
        switch(TestPhase)
        {
            case 0: foreach(var e in enemies)if(e)e.enabled=false;TestFlags=1;NextPhase(1);break;
            case 1: if(t>1.2f){if(Game.warrior.transform.position.x<1&&Game.mage.transform.position.x>5&&sawJump)TestFlags|=2;Game.warrior.Teleport(new Vector3(4.3f,1.3f,0));Game.mage.Teleport(new Vector3(9,1.3f,0));NextPhase(2);}break;
            case 2: if(t>.8f)NextPhase(3);break;
            case 3: if(t>1){if(chests[0].opened)TestFlags|=4;Game.warrior.Teleport(new Vector3(6,1.3f,0));NextPhase(20);}break;
            case 20: if(t>.7f){if(Game.warrior.hasEquipment&&Game.warrior.armor==60)TestFlags|=2048;Game.warrior.immuneUntil=0;Game.warrior.Damage(1000);chests.First(c=>c.reward=="Salud").Hit();NextPhase(4);}break;
            case 4: if(t>1){if(Game.warrior.IsDown)TestFlags|=8;Game.mage.Teleport(new Vector3(20,1.8f,0));NextPhase(5);}break;
            case 5: if(t>1){if(Game.warrior.health==50&&Game.mage.health==100)TestFlags|=16;Game.warrior.hasEquipment=true;Game.warrior.armor=60;Game.warrior.Teleport(new Vector3(137.35f,1.3f,0));Game.mage.Teleport(new Vector3(135.8f,1.3f,0));NextPhase(34);}break;
            case 6: if(t>.65f){if(Game.gateOpen&&liftGateSequence)TestFlags|=32;Game.warrior.Teleport(new Vector3(20,1.3f,0));Game.mage.Teleport(new Vector3(22,1.3f,0));Game.mage.fireCharge=Game.mage.iceCharge=12;Game.mage.protectionCharge=25;Game.mage.selectedItem=0;Game.mage.itemActive=false;Game.mage.enchantment="Ninguno";NextPhase(7);}break;
            case 34:
                if (t > .45f)
                {
                    liftGateSequence = !Game.Lift.Carrying && !Game.gateOpen;
                    Game.Lift.Cancel();
                    NextPhase(35);
                }
                break;
            case 35:
                if (t > .55f)
                {
                    liftGateSequence &= Game.Lift.Requester == 1 && !Game.Lift.Carrying;
                    NextPhase(36);
                }
                break;
            case 36:
                if (t > 1.95f)
                {
                    liftGateSequence &= Game.Lift.Carrying && Game.warrior.transform.position.y > 5.8f && !Game.gateOpen;
                    NextPhase(37);
                }
                break;
            case 37:
                if (t > .86f) NextPhase(38);
                break;
            case 38:
                if (t > .4f)
                {
                    liftGateSequence &= !Game.Lift.Carrying && Game.Lift.ReleasedFromLift;
                    NextPhase(6);
                }
                break;
            case 7: if(t>1){if(Game.warrior.crouching&&Game.warrior.blocking)TestFlags|=64;if(!Game.mage.itemActive&&Game.mage.fireCharge==11)TestFlags|=256;NextPhase(8);}break;
            case 8: if(t>1){int hp=Game.warrior.health;Game.warrior.immuneUntil=0;Game.warrior.Damage(20,true,Game.warrior.transform.position.x,Game.warrior.transform.position.y+4);if(Game.warrior.shieldUp&&Game.warrior.health==hp)TestFlags|=128;NextPhase(9);}break;
            case 9: if(t>1){if(Game.mage.selectedItem==1&&!Game.mage.itemActive&&Game.mage.fireCharge==11)TestFlags|=512;NextPhase(10);}break;
            case 10: if(t>1){Game.warrior.Teleport(new Vector3(40.5f,.8f,0));NextPhase(11);}break;
            case 11: if(t>1.15f){if(Game.warrior.transform.position.x>41.7f&&Game.warrior.transform.position.y>.5f&&Game.mage.selectedItem==1&&Game.mage.iceCharge==11)TestFlags|=1024;NextPhase(12);}break;
            case 12:
                foreach(var e in enemies)if(e)e.enabled=false;
                var pursuit=enemies.First(e=>e&&!e.ranged&&!e.whip);pursuit.enabled=true;PlaceEnemy(pursuit,new Vector3(78,1.15f,0));
                Game.warrior.Teleport(new Vector3(72,1.3f,0));Game.mage.Teleport(new Vector3(70,1.3f,0));
                Game.warrior.health=100;Game.warrior.armor=0;Game.warrior.immuneUntil=Time.time+100;NextPhase(13);break;
            case 13: if(t>3.1f){var e=enemies.First(e=>e&&!e.ranged&&!e.whip);pursuitCheck=e.State+" x="+e.transform.position.x+" target="+(e.Target?e.Target.transform.position.ToString():"none");if(e.transform.position.x<75)TestFlags|=4096;e.enabled=false;
                Game.warrior.Teleport(new Vector3(47.3f,1.3f,0));Game.mage.Teleport(new Vector3(100,1.3f,0));
                var a=enemies.First(e=>e&&e.ranged);a.enabled=true;archerAttacks=a.AttackCount;NextPhase(14);}break;
            case 14: if(t>1.2f){var a=enemies.First(e=>e&&e.ranged);if(a.AttackCount>archerAttacks&&Mathf.Abs(a.transform.position.x-43)<.01f)TestFlags|=8192;a.enabled=false;
                foreach(var b in FindObjectsByType<VillageEnemyBolt>())Destroy(b.gameObject);
                var boss=enemies.First(e=>e&&e.whip);boss.enabled=true;Game.warrior.Teleport(new Vector3(111.2f,1.3f,0));
                Game.warrior.health=100;Game.warrior.immuneUntil=0;Game.mage.Teleport(new Vector3(99,1.3f,0));NextPhase(15);}break;
            case 15: if(t>.25f){whipWarned=enemies.First(e=>e&&e.whip).AttackVisual>0&&Game.warrior.health==100;NextPhase(16);}break;
            case 16: if(t>.95f){var boss=enemies.First(e=>e&&e.whip);if(whipWarned&&Game.warrior.health==75)TestFlags|=16384;boss.Hit(999,false);NextPhase(17);}break;
            case 21: if(t>.4f){int hp=Game.warrior.health;Game.warrior.immuneUntil=0;Game.warrior.Damage(20,true,Game.warrior.transform.position.x+Game.warrior.Facing*3,Game.warrior.transform.position.y);if(!Game.warrior.hasEquipment&&Game.warrior.armor==0&&Game.warrior.health==hp)TestFlags|=65536;NextPhase(22);}break;
            case 22: if(t>.6f){var e=enemies.First(e=>e&&!e.ranged&&!e.whip);e.enabled=true;e.SetHome(Game.SafeSpawn(202));Game.warrior.Teleport(new Vector3(20,1.3f,0));Game.mage.Teleport(new Vector3(22,1.3f,0));NextPhase(23);}break;
            case 23: if(t>2){var e=enemies.First(e=>e&&!e.ranged&&!e.whip);if(Mathf.Abs(e.transform.position.x-202)>.4f&&e.State=="Patrulla")TestFlags|=131072;e.enabled=false;var boss=enemies.First(e=>e&&e.whip);boss.health=240;boss.gameObject.SetActive(true);boss.enabled=true;boss.SetHome(new Vector3(114,1.65f,0));Game.warrior.Teleport(new Vector3(119.5f,1.3f,0));Game.warrior.immuneUntil=Time.time+10;Game.mage.Teleport(new Vector3(3,1.3f,0));NextPhase(24);}break;
            case 24: if(t>1.2f){Game.warrior.Teleport(new Vector3(140,1.3f,0));NextPhase(25);}break;
            case 25: if(t>2.8f){var boss=enemies.First(e=>e&&e.whip);if(boss.HomeReturns>0&&Mathf.Abs(boss.transform.position.x-114)<2.5f)TestFlags|=262144;NextPhase(26);}break;
            case 26:
                foreach(var star in stars){star.GetComponent<Collider>().enabled=false;star.mask=0;}
                Game.warrior.stars=Game.mage.stars=Game.warrior.extraLives=Game.mage.extraLives=0;Game.warrior.health=Game.mage.health=100;
                Game.warrior.Teleport(new Vector3(20,1.3f,0));Game.mage.Teleport(new Vector3(120,1.3f,0));Game.mage.immuneUntil=Time.time+100;
                for(int i=0;i<100;i++)stars[i].TryCollect(Game.warrior);
                if(Game.warrior.stars==0&&Game.warrior.extraLives==1&&Game.mage.extraLives==0)TestFlags|=524288;NextPhase(27);break;
            case 27: if(t>.6f){for(int i=0;i<100;i++)stars[i].TryCollect(Game.mage);bool blocked=Game.mage.stars==0&&Game.mage.extraLives==0;stars[100].TryCollect(Game.mage);if(blocked&&Game.mage.stars==1&&stars.Take(100).All(s=>s.mask==1))TestFlags|=1048576;
                Game.warrior.armor=0;Game.warrior.hasEquipment=false;Game.warrior.immuneUntil=0;Game.warrior.Damage(1000);NextPhase(28);}break;
            case 28: if(t>.6f){if(Game.warrior.health==100&&Game.warrior.extraLives==0&&Game.mage.extraLives==0)TestFlags|=2097152;
                Game.warrior.immuneUntil=0;Game.warrior.Damage(10);NextPhase(29);}break;
            case 29: if(t>1.1f){Game.warrior.Damage(10);if(Game.warrior.health==90&&Game.warrior.RecoveryRemaining>1.7f)TestFlags|=4194304;NextPhase(30);}break;
            case 30: if(t>2.1f){Game.warrior.Damage(10);if(Game.warrior.health==80)TestFlags|=8388608;
                Game.mage.selectedItem=2;Game.mage.protectionCharge=25;Game.mage.itemActive=false;Game.mage.health=100;Game.mage.immuneUntil=0;Game.mage.ToggleItem();NextPhase(31);}break;
            case 31: if(t>.6f){Game.mage.Damage(20,true);Game.mage.Damage(50);if(Game.mage.ProtectionVisible&&Game.mage.health==100)TestFlags|=16777216;Game.mage.SelectItem(1);Game.mage.immuneUntil=0;Game.mage.Damage(10);NextPhase(32);}break;
            case 32: if(t>.6f){if(!Game.mage.ProtectionVisible&&Game.mage.health==90)TestFlags|=33554432;crouchAttack=Game.mage.AttackSerial;NextPhase(33);}break;
            case 33: if(t>.6f){if(Game.mage.crouching&&Game.mage.AttackSerial>crouchAttack)TestFlags|=67108864;
                foreach (var enemy in enemies) if (enemy) enemy.gameObject.SetActive(false);
                Game.warrior.Teleport(new Vector3(120,1.3f,0));Game.mage.Teleport(new Vector3(121.4f,1.3f,0));
                NextPhase(39);}break;
            case 39:
                if (t > .45f) NextPhase(40);
                break;
            case 40:
                if (t > .45f && Game.Lift.Carrying) sawLiftTimeout = true;
                if (t > 7.3f)
                {
                    sawLiftTimeout &= !Game.Lift.Carrying && Game.Lift.ReleasedFromLift;
                    if (sawLiftTimeout) TestFlags |= 134217728;
                    Game.warrior.Teleport(new Vector3(116,1.3f,0));
                    NextPhase(42);
                }
                break;
            case 42:
                if (t > .65f) NextPhase(43);
                break;
            case 43:
                if (t > .5f && Game.Lift.Requester == 2 && !Game.Lift.Carrying) sawLiftReject = true;
                if (t > 9.65f)
                {
                    if (sawLiftReject && Game.Lift.Requester == 0 && !Game.Lift.Carrying) TestFlags |= 268435456;
                    Game.warrior.Teleport(new Vector3(166.64f, 1.25f, 0));
                    Game.mage.Teleport(new Vector3(166.64f, 1.25f, 0));
                    NextPhase(79);
                }
                break;
            case 79:
                if (t > .45f) NextPhase(70);
                break;
            case 70:
                if (t > 1.85f)
                {
                    liftGateSequence = BothNearHeight(2.6f);
                    NextPhase(71);
                }
                break;
            case 71:
                if (t > 1.3f)
                {
                    liftGateSequence &= BothNearHeight(4.1f) && Game.warrior.transform.position.x > 177
                        && Game.mage.transform.position.x > 177;
                    NextPhase(72);
                }
                break;
            case 72:
                if (t > 3.4f)
                {
                    if (liftGateSequence && Game.warrior.transform.position.x > 193 && Game.mage.transform.position.x > 193)
                        TestFlags |= 536870912;
                    else Debug.LogWarning($"Fortress route: {liftGateSequence}, {Game.warrior.transform.position}, {Game.mage.transform.position}");
                    testDagger = enemies.First(e => e.daggerThrower);
                    testDagger.gameObject.SetActive(true);
                    testDagger.enabled = true;
                    testDagger.SetHome(new Vector3(119.5f, 1.15f, 0));
                    Game.warrior.Teleport(new Vector3(114.6f, 1.3f, 0));
                    Game.mage.Teleport(new Vector3(113.1f, 1.3f, 0));
                    Game.warrior.armor = 60;
                    Game.warrior.hasEquipment = true;
                    Game.warrior.health = 100;
                    Game.warrior.immuneUntil = 0;
                    NextPhase(60);
                }
                break;
            case 60:
                if (t > 1.65f)
                {
                    bool tookDagger = testDagger.AttackCount > 0 && Game.warrior.health == 93 && Game.warrior.armor == 53;
                    daggerSequence = tookDagger;
                    testDagger.enabled = false;
                    NextPhase(61);
                }
                break;
            case 61:
            case 63:
            case 65:
                if (t > 1.05f)
                {
                    int expectedShield = 60 - (TestPhase - 59) * 10;
                    daggerSequence &= testDagger.health == 75 && testDagger.shieldHealth == expectedShield;
                    if (testDagger.shieldHealth != expectedShield)
                        Debug.LogWarning($"Dagger shield phase {TestPhase}: expected {expectedShield}, got {testDagger.shieldHealth}");
                    NextPhase(TestPhase + 1);
                }
                break;
            case 62:
            case 64:
            case 66:
                if (t > .28f) NextPhase(TestPhase + 1);
                break;
            case 67:
                if (t > 1.05f)
                {
                    if (daggerSequence && testDagger.health == 55 && testDagger.shieldHealth == 0)
                        TestFlags |= DaggerCheck;
                    Game.warrior.health = 100;
                    Game.warrior.armor = 10;
                    Game.warrior.immuneUntil = 0;
                    Game.warrior.Damage(20);
                    bool tookHalf = Game.warrior.health == 90 && Game.warrior.armor == 0 && !Game.warrior.hasEquipment;
                    Game.warrior.immuneUntil = 0;
                    Game.warrior.Damage(20);
                    if (tookHalf && Game.warrior.health == 70) TestFlags |= ArmorCheck;
                    testDagger.gameObject.SetActive(false);
                    PrepareSecret(false);
                    NextPhase(89);
                }
                break;
            case 89:
            case 99:
                if (t > .4f) NextPhase(TestPhase + 1);
                break;
            case 90:
            case 100:
                highestUnassisted = Mathf.Max(highestUnassisted, Game.warrior.transform.position.y);
                if (t > 1f) NextPhase(TestPhase + 1);
                break;
            case 91:
            case 101:
                if (t > .6f) NextPhase(TestPhase + 1);
                break;
            case 92:
            case 102:
                if (t > 2.7f) NextPhase(TestPhase + 1);
                break;
            case 93:
            case 103:
                if (t > 1.3f) NextPhase(TestPhase + 1);
                break;
            case 94:
            case 104:
                if (t > .95f) NextPhase(TestPhase + 1);
                break;
            case 95:
            case 105:
                if (t > .55f) NextPhase(TestPhase + 1);
                break;
            case 96:
            case 106:
                if (t > 1.15f)
                {
                    if (highestUnassisted < testSecret.transform.position.y && testSecret.opened && !testSecret.LootActive
                        && Game.Lift.ReleasedFromLift && (TestPhase == 96 ? Game.warrior.armor == 60 : Game.warrior.extraLives == 1))
                        secretsRecovered++;
                    else Debug.LogWarning($"Secret {TestPhase}: {Game.warrior.transform.position}, opened={testSecret.opened}, loot={testSecret.LootActive}, jump={highestUnassisted}, released={Game.Lift.ReleasedFromLift}");
                    if (TestPhase == 96)
                    {
                        PrepareSecret(true);
                        NextPhase(99);
                    }
                    else NextPhase(18);
                }
                break;
            case 17: if(t>.5f){if(!enemies.First(e=>e&&e.whip).gameObject.activeSelf)TestFlags|=32768;Game.warrior.hasEquipment=true;Game.warrior.armor=10;Game.warrior.Teleport(new Vector3(20,1.3f,0));NextPhase(21);}break;
        }
    }
    bool BothNearHeight(float center) =>
        Mathf.Abs(Game.warrior.transform.position.y - center) < .25f
        && Mathf.Abs(Game.mage.transform.position.y - center) < .25f
        && Game.warrior.Grounded && Game.mage.Grounded;

    void PrepareSecret(bool inner)
    {
        float entry = inner ? 171.9f : 116.7f;
        testSecret = chests.Single(chest => chest.keepOnRetry && chest.reward == (inner ? "Vida extra" : "Armadura"));
        Game.Lift.Cancel();
        Game.warrior.Teleport(Game.SafeSpawn(entry - .62f, 4.8f));
        Game.mage.Teleport(Game.SafeSpawn(entry - 1.8f, 4.8f));
        highestUnassisted = 0;
    }

    static void PlaceEnemy(VillageEnemy e,Vector3 position)
    {e.SetHome(position);}
    void ObserveTest()
    {
        if(TestPhase==1&&Heroes[0].Position.x<1&&Heroes[1].Position.x>5)sawRemoteMovement=true;
        if(chests[0].opened)sawChest=true;
        if(Game.warrior.IsDown)sawDown=true;
        if(sawDown&&Game.warrior.health==50)sawRevive=true;
        if(Game.warrior.hasEquipment)sawEquipment=true;
        if(Game.warrior.crouching&&Game.warrior.blocking)sawCrouch=true;
        if(Game.warrior.shieldUp)sawUpperShield=true;
        if(!Game.mage.itemActive&&Game.mage.fireCharge==11)sawItemUse=true;
        if(sawItemUse&&Game.mage.selectedItem==1&&Game.mage.iceCharge==11)sawItemSwitch=true;
        if(TestPhase==13&&enemies.Any(e=>e&&!e.ranged&&!e.whip&&e.transform.position.x>72&&e.transform.position.x<75))sawEnemyChase=true;
        if(TestPhase==14&&Projectiles.Any(s=>s.Active&&s.Arrow))sawArrow=true;
        if((TestPhase==15||TestPhase==16)&&enemies.Any(e=>e&&e.whip&&e.AttackVisual>0))sawWhip=true;
        if(TestPhase>=17&&enemies.Any(e=>e&&e.whip&&!e.gameObject.activeSelf))sawEnemyDeath=true;
        if(TestPhase==22&&!Game.warrior.hasEquipment&&Game.warrior.armor==0)sawGearBreak=true;
        if(TestPhase==27&&Game.warrior.extraLives==1&&Game.mage.extraLives==0&&StarMasks.Take(100).All(x=>x==1))sawPersonalStars=true;
        if(TestPhase>=28&&StarMasks.Take(100).All(x=>x==1)&&Game.mage.stars==1)sawSharedStars=true;
        if(TestPhase==28&&Game.warrior.health==100&&Game.warrior.extraLives==0)sawExtraLife=true;
        if(TestPhase==29&&Game.warrior.health==90&&Game.warrior.RecoveryRemaining>0)sawRecovery=true;
        if(TestPhase==31&&Game.mage.ProtectionVisible)sawProtectionField=true;
        if(TestPhase==33&&Game.mage.crouching&&Game.mage.AttackSerial>0)sawMageCrouch=true;
        if (TestPhase == 35 && Game.Lift.Requester == 1 && Game.Lift.RequestLeft > 8) sawLiftRequest = true;
        if (TestPhase == 36 && Game.Lift.Carrying && Game.warrior.transform.position.y > 3) sawLiftCarry = true;
        if ((TestPhase == 38 || TestPhase == 6) && sawLiftCarry && !Game.Lift.Carrying) sawLiftRelease = true;
        if (TestPhase == 60 && Projectiles.Any(shot => shot.Active && shot.Dagger)) sawDagger = true;
        if ((TestPhase == 66 || TestPhase == 67) && enemies.Any(e => e.daggerThrower && e.gameObject.activeSelf && e.shieldHealth == 0))
            sawBrokenEnemyShield = true;
        if(TestPhase!=18||reported)return;reported=true;
        var lines=new List<string>{"Role: "+(HasStateAuthority?"Host":"Client"),"Players: "+Runner.ActivePlayers.Count(),"Transport: Photon Fusion Host/Client", "Host flags: "+TestFlags};
        lines.Add(TestFlags == -1?"PASS host: connection, inputs, chest, revival, gate, postures, inventory, slope, pursuit, arrows, whip, enemy death":"FAIL host checks");
        lines.Add(sawRemoteMovement?"PASS both player positions replicated":"FAIL movement replication");
        lines.Add(sawChest?"PASS chest opening replicated":"FAIL chest replication");
        lines.Add(sawDown&&sawRevive?"PASS defeat and revival replicated":"FAIL revival replication");
        lines.Add(Game.gateOpen?"PASS cooperative gate replicated":"FAIL gate replication");
        lines.Add(sawEquipment?"PASS starting equipment pickup replicated":"FAIL equipment replication");
        lines.Add(sawCrouch&&sawUpperShield?"PASS crouch and overhead shield replicated":"FAIL posture replication");
        lines.Add(sawItemUse&&sawItemSwitch?"PASS item selection, activation and remaining charge replicated":"FAIL inventory replication");
        lines.Add(sawEnemyChase?"PASS pursuing enemy position replicated":"FAIL enemy pursuit replication · "+pursuitCheck);
        lines.Add(sawArrow?"PASS hill archer arrow replicated":"FAIL arrow replication");
        lines.Add(sawWhip?"PASS whip attack warning replicated":"FAIL whip replication");
        lines.Add(sawEnemyDeath?"PASS defeated guardian removed on both peers":"FAIL enemy death replication");
        lines.Add(sawGearBreak?"PASS equipment wear and loss replicated":"FAIL equipment wear replication");
        lines.Add((TestFlags&393216)==393216?"PASS patrol and guardian return on host":"FAIL patrol or guardian return");
        lines.Add(sawPersonalStars?"PASS personal star counter replicated":"FAIL personal stars");
        lines.Add(sawSharedStars?"PASS shared stars disappear and cannot be collected again":"FAIL shared-star collection");
        lines.Add(sawExtraLife?"PASS extra-life input and revival replicated":"FAIL extra-life replication");
        lines.Add(sawRecovery?"PASS damage recovery replicated":"FAIL recovery replication");
        lines.Add(sawProtectionField?"PASS round mage protection visible on both peers":"FAIL protection field replication");
        lines.Add(sawMageCrouch?"PASS mage crouches and fires on both peers":"FAIL mage crouch replication");
        lines.Add(sawLiftRequest && sawLiftCarry && sawLiftRelease && (TestFlags & 32) != 0
            ? "PASS sequential lift request, carried movement, release and elevated seal"
            : "FAIL cooperative lift or elevated seal");
        lines.Add((TestFlags & 134217728) != 0 ? "PASS automatic release after seven seconds" : "FAIL lift timeout");
        lines.Add((TestFlags & 268435456) != 0 ? "PASS out-of-range acceptance rejected and request expires after ten seconds" : "FAIL request timeout");
        lines.Add((TestFlags & 536870912) != 0 ? "PASS both players walk the fortress with one threshold jump" : "FAIL fortress traversal");
        lines.Add((TestFlags & DaggerCheck) != 0 && sawDagger && sawBrokenEnemyShield
            ? "PASS ranged dagger hit, three blocked magic shots break shield, fourth hits guard"
            : "FAIL dagger attack or shield replication");
        lines.Add((TestFlags & ArmorCheck) != 0 && Game.warrior.health == 70
            ? "PASS 20 damage costs 10 HP with armor and 20 HP after it breaks"
            : "FAIL armor mitigation");
        bool secretsSynced = chests.Count(chest => chest.keepOnRetry && chest.opened && !chest.LootActive) == 2
            && Game.warrior.armor == 60 && Game.warrior.extraLives == 1;
        lines.Add(secretsSynced && (!HasStateAuthority || secretsRecovered == 2)
            ? "PASS both secret chambers reached with the lift, opened and looted"
            : "FAIL secret chamber access, loot or replication");
        var path=VillageOnline.Instance.ReportPath;if(!string.IsNullOrEmpty(path)){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,lines);}
        Debug.Log(string.Join("\n",lines));
#if UNITY_EDITOR
        if(!string.IsNullOrEmpty(path))ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(path),"online-v4-hud.png"));
#endif
    }
}
