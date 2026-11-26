using UnityEngine;

namespace Kingdoms.Survival
{
    public sealed class SurvivalPickup : MonoBehaviour
    {
        public SurvivalArena arena;
        public SupplyKind kind;
        public int id,collector;
        public bool available=true,rechargeEachRest,reserveDrop,bonusStar;
        public Transform art;
        Renderer[] renderers;
        Vector3 basePosition,initialPosition;TextMesh label;int labelRound=-1;
        void Awake()
        {
            if(kind==SupplyKind.Health&&art&&!art.Find("PocionSalud"))
            {
                var prefab=Resources.Load<GameObject>("VillageProps/PocionSalud");
                if(prefab){foreach(Transform child in art)child.gameObject.SetActive(false);var potion=Instantiate(prefab,art,false);potion.name="PocionSalud";}
            }
            initialPosition=transform.position;basePosition=art?art.localPosition:Vector3.zero;renderers=GetComponentsInChildren<Renderer>(true);if(reserveDrop)available=false;
        }
        public bool TryCollect(SurvivalHero h)
        {
            if(!available||reserveDrop||h.Down||!arena.Authoritative)return false;
            bool accepted=true;
            switch(kind)
            {
                case SupplyKind.Star:h.AddStars(1);break;
                case SupplyKind.Armor:accepted=!h.IsMage&&(!h.equipped||h.armor<60);if(accepted){h.armor=Mathf.Min(60,h.armor+SurvivalSupplyRules.ArmorGain(arena.SupplyRound));h.equipped=true;}break;
                case SupplyKind.Fire:accepted=h.IsMage&&h.fire<12;if(accepted)h.fire=Mathf.Min(12,h.fire+SurvivalSupplyRules.BookCharges(arena.SupplyRound));break;
                case SupplyKind.Ice:accepted=h.IsMage&&h.ice<12;if(accepted)h.ice=Mathf.Min(12,h.ice+SurvivalSupplyRules.BookCharges(arena.SupplyRound));break;
                case SupplyKind.Protection:accepted=h.IsMage&&h.protection<25;if(accepted)h.protection=Mathf.Min(25,h.protection+SurvivalSupplyRules.ProtectionSeconds(arena.SupplyRound));break;
                case SupplyKind.WarriorLife:accepted=!h.IsMage;if(accepted)h.lives++;break;
                case SupplyKind.MageLife:accepted=h.IsMage;if(accepted)h.lives++;break;
                case SupplyKind.Health:accepted=arena.CollectHealth(h);break;
            }
            if(!accepted)return false;
            available=false;collector=h.id+1;return true;
        }
        public void Recharge(){available=true;collector=0;}
        public void ResetForMatch(){available=!reserveDrop&&!bonusStar;collector=0;transform.position=initialPosition;}
        void LateUpdate()
        {
            if(renderers==null)renderers=GetComponentsInChildren<Renderer>(true);
            if(arena&&kind!=SupplyKind.Star&&!reserveDrop&&labelRound!=arena.SupplyRound)
            {
                labelRound=arena.SupplyRound;if(!label)label=GetComponentInChildren<TextMesh>(true);
                if(label)switch(kind)
                {
                    case SupplyKind.Health:label.text="SALUD +"+arena.HealthRestore;break;
                    case SupplyKind.Armor:label.text="ARMADURA +"+SurvivalSupplyRules.ArmorGain(labelRound);break;
                    case SupplyKind.Fire:label.text="FUEGO +"+SurvivalSupplyRules.BookCharges(labelRound);break;
                    case SupplyKind.Ice:label.text="HIELO +"+SurvivalSupplyRules.BookCharges(labelRound);break;
                    case SupplyKind.Protection:label.text="PROT +"+SurvivalSupplyRules.ProtectionSeconds(labelRound)+"s";break;
                }
            }
            foreach(var r in renderers)if(r)r.enabled=available;
            if(art&&kind==SupplyKind.Star){art.localPosition=basePosition+Vector3.up*Mathf.Sin(Time.time*2.4f+id)*.07f;art.Rotate(Vector3.up,90*Time.deltaTime,Space.World);}
        }
    }
}
