using System.Linq;
using UnityEngine;

namespace Kingdoms.Survival
{
    public sealed class SurvivalHeroView : MonoBehaviour
    {
        public SurvivalHero hero;
        GameObject root;
        VillageWarriorVisual warrior;
        VillageMageVisual mage;
        Animator active;
        GameObject bubble;
        string state;
        int serial;
        float impactUntil;
        int previousArmor;
        public void Initialize()
        {
            var prefab=Resources.Load<GameObject>(hero.IsMage?"VillageMageVisual":"VillageWarriorVisual");
            if(!prefab)return;
            root=Instantiate(prefab,transform,false);root.transform.localScale=Vector3.one*1.1f;
            warrior=root.GetComponent<VillageWarriorVisual>();if(warrior)warrior.enabled=false;
            mage=root.GetComponent<VillageMageVisual>();if(mage)mage.enabled=false;
            foreach(var old in root.GetComponentsInChildren<SwordSidePreview>(true))old.enabled=false;
            bubble=GameObject.CreatePrimitive(PrimitiveType.Sphere);bubble.name="Protección redonda del mago";bubble.transform.SetParent(transform,false);bubble.transform.localPosition=Vector3.up;bubble.transform.localScale=Vector3.one*2.6f;var c=bubble.GetComponent<Collider>();c.enabled=false;Destroy(c);bubble.GetComponent<Renderer>().sharedMaterial=hero.arena.protection;bubble.SetActive(false);
        }
        public void Impact(){impactUntil=Time.time+.36f;}
        void LateUpdate()
        {
            if(!root)return;
            if(warrior&&previousArmor>0&&hero.armor==0&&!hero.equipped&&!hero.Down)warrior.BeginArmorBreak();
            if(hero.guarding&&hero.armor<previousArmor)impactUntil=Time.time+.36f;
            previousArmor=hero.armor;
            root.transform.localRotation=Quaternion.LookRotation(hero.heading.sqrMagnitude>.01f?hero.heading:Vector3.forward);
            Animator next;
            bool breaking=warrior&&!hero.Down&&!hero.equipped&&warrior.ArmorBreakPlaying;
            bool jogging=warrior&&hero.equipped&&!hero.Down&&!hero.airborne&&!hero.guarding&&hero.attackAge>=.48f&&hero.moveSpeed>.15f;
            bool library=breaking||jogging||(!hero.IsMage&&hero.equipped&&!hero.Down&&hero.attackAge<.48f&&warrior.libraryAttack);
            bool punch=warrior&&!hero.equipped&&!hero.Down&&!breaking&&hero.attackAge<warrior.punchDuration&&warrior.libraryBare;
            if(hero.IsMage)next=mage.animator;
            else
            {
                warrior.bare.gameObject.SetActive(!hero.equipped&&!library&&!punch);
                warrior.equipped.gameObject.SetActive(hero.equipped&&!library);
                if(warrior.libraryAttack)warrior.libraryAttack.gameObject.SetActive(library);
                if(warrior.libraryBare)warrior.libraryBare.gameObject.SetActive(punch);
                warrior.SetLibraryGear(!breaking);
                next=library?warrior.libraryAttack:punch?warrior.libraryBare:hero.equipped?warrior.equipped:warrior.bare;
            }
            if(active!=next){active=next;state=null;}
            bool changed=serial!=hero.attackSerial;serial=hero.attackSerial;
            bool moving=hero.moveSpeed>.15f;
            string clip;
            if(hero.IsMage)clip=hero.Down?"CrouchIdle":hero.attackAge<.36f?(hero.airborne?"AirCast":"Cast"):hero.airborne?"Jump":moving?"Walk":"Idle";
            else if(breaking)clip="ShieldBreak";
            else if(jogging)clip="JogEquipped";
            else if(library)clip="SwordAttack";
            else if(punch)clip="PunchJab";
            else if(hero.Down)clip=hero.equipped?"CrouchIdle":"CrouchIdleBare";
            else if(hero.airborne&&hero.attackAge>=.3f)clip=hero.equipped?"JumpEquipped":"Jump";
            else if(hero.attackAge<.3f)clip=hero.equipped?"SwordSlash":"Punch";
            else if(hero.guarding)clip=Time.time<impactUntil?"BlockImpact":"Guard";
            else clip=moving?(hero.equipped?"RunEquipped":"Run"):(hero.equipped?"ReadyEquipped":"Ready");
            if(state!=clip||(changed&&hero.attackAge<.5f))
            {if(state==null)active.Play(clip,0,0);else active.CrossFadeInFixedTime(clip,.08f,0,0);active.Update(0);state=clip;}
            if(library&&active.isHuman)warrior.AlignLibraryPose(hero.heading,jogging);
            else if(punch&&active.isHuman)SwordSidePreview.AlignAttack(active,hero.heading);
            bubble.SetActive(hero.IsMage&&hero.protectedByMagic&&!hero.Down);
            bool flash=!hero.Down&&Time.time<hero.immuneUntil&&Mathf.FloorToInt(Time.time*10)%2==0;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",hero.Down?new Color(.45f,.45f,.45f):flash?new Color(1,.55f,.5f):Color.white);
            foreach(var r in root.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(block);
        }
    }

    public sealed class SurvivalEnemyView : MonoBehaviour
    {
        public SurvivalEnemy enemy;
        GameObject model,shield;
        Animator animator;
        string state;
        int attack,health,resistance;
        float heldUntil;
        Transform mace;
        LineRenderer danger;
        FrozenEnemyVisual freezeVisual;
        Vector3 previousPosition;
        public void Initialize()
        {
            var prefab=Resources.Load<GameObject>(enemy.Bomber?(enemy.id%2==1?"VillageBomber1Visual":"VillageBomberVisual"):enemy.kind==FoeKind.Goblin?"VillageGoblinVisual":enemy.kind==FoeKind.Dagger?"VillageDaggerVisual":"VillageEnemy1Visual");
            model=Instantiate(prefab,transform,false);
            var dagger=model.GetComponent<VillageDaggerVisual>();
            if(dagger){animator=dagger.animator;shield=dagger.shield;dagger.enabled=false;model.transform.localRotation=Quaternion.Euler(0,180,0);}
            else {var normal=model.GetComponent<VillageEnemyVisual>();animator=normal.animator;normal.enabled=false;}
            model.transform.localScale=Vector3.one*(enemy.Giant?1.95f:enemy.kind==FoeKind.Goblin?1f:1.05f);health=enemy.health;resistance=enemy.shieldHealth;previousPosition=transform.position;
            if(enemy.Giant)
            {
                var pivot=new GameObject("Maza · pivote");pivot.transform.SetParent(transform,false);pivot.transform.localPosition=new Vector3(.65f,2.4f,.25f);mace=pivot.transform;
                Piece("Mango",PrimitiveType.Cylinder,new Vector3(0,-.65f,0),new Vector3(.14f,.85f,.14f),enemy.arena.stone,mace);
                Piece("Cabeza de maza",PrimitiveType.Sphere,new Vector3(0,-1.4f,0),Vector3.one*.85f,enemy.arena.steel,mace);
                for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;Piece("Punta de maza",PrimitiveType.Cube,new Vector3(Mathf.Cos(angle)*.4f,-1.4f,Mathf.Sin(angle)*.4f),Vector3.one*.22f,enemy.arena.steel,mace);}
            }
            var warning=new GameObject("Aviso del ataque");warning.transform.SetParent(transform,false);danger=warning.AddComponent<LineRenderer>();danger.loop=true;danger.positionCount=64;danger.widthMultiplier=enemy.Giant?.14f:.06f;danger.sharedMaterial=enemy.arena.warning;
            freezeVisual=gameObject.AddComponent<FrozenEnemyVisual>();
        }
        static void Piece(string name,PrimitiveType type,Vector3 p,Vector3 s,Material mat,Transform parent)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=mat;var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);}
        void LateUpdate()
        {
            if(!animator)return;
            freezeVisual.Present(enemy.IsFrozen);
            var movement=transform.position-previousPosition;previousPosition=transform.position;
            if(enemy.IsFrozen){danger.enabled=false;health=enemy.health;resistance=enemy.shieldHealth;attack=enemy.attackSerial;return;}
            transform.rotation=Quaternion.LookRotation(enemy.heading.sqrMagnitude>.01f?enemy.heading:Vector3.back);
            if(shield)shield.SetActive(enemy.shieldHealth>0);
            string next=null;
            if(enemy.health<=0)next="Fall";
            else if(enemy.shieldHealth<resistance&&enemy.shieldHealth>0){next="ShieldImpact";heldUntil=Time.time+.37f;}
            else if(enemy.health<health){next="Hit";heldUntil=Time.time+.4f;}
            else if(attack!=enemy.attackSerial){next=enemy.kind==FoeKind.Dagger?"Throw":"Attack";heldUntil=Time.time+(enemy.Giant?1.25f:.75f);}
            else if(Time.time>=heldUntil)next=enemy.moveSpeed>.1f?enemy.Bomber&&Vector3.Dot(movement,enemy.heading)<0?"WalkBack":"Walk":"Idle";
            health=enemy.health;resistance=enemy.shieldHealth;attack=enemy.attackSerial;
            if(next!=null&&next!=state){if(state==null||next=="Fall")animator.Play(next,0,0);else animator.CrossFadeInFixedTime(next,.065f,0,0);state=next;}
            danger.enabled=enemy.preparing&&enemy.health>0;
            if(danger.enabled)
            {
                var center=transform.position+enemy.heading*(enemy.Giant?2.4f:1);
                float radius=enemy.Giant?3.6f:1.4f;
                for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;danger.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,.06f,Mathf.Sin(a)*radius));}
            }
            if(mace)mace.localRotation=Quaternion.Euler(enemy.preparing?-Mathf.Lerp(30,120,1-enemy.windupRemaining/1.2f):20,0,-20);
        }
    }
}
