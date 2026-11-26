using UnityEngine;
namespace Kingdoms.Survival
{
    // Values affect new pickups only; saved health, lives, armor and charges are never taken away.
    public static class SurvivalSupplyRules
    {
        static readonly int[] health={6,5,4,3,2},armor={5,5,4,4,3},lives={2,1,1,0,0};
        static readonly int[] fire={3,3,3,3,2},ice={3,3,3,2,3},protection={3,3,2,3,2};
        static int Index(int round)=>Mathf.Clamp(round,1,5)-1;
        public static int HealthGain(int round)=>35-5*Index(round);
        public static int ArmorGain(int round)=>60-4*Index(round);
        public static int BookCharges(int round)=>12-Index(round);
        public static int ProtectionSeconds(int round)=>25-2*Index(round);
        public static int Budget(SupplyKind kind,int round)
        {
            int i=Index(round);
            switch(kind)
            {
                case SupplyKind.Health:return health[i];case SupplyKind.Armor:return armor[i];
                case SupplyKind.Fire:return fire[i];case SupplyKind.Ice:return ice[i];case SupplyKind.Protection:return protection[i];
                case SupplyKind.WarriorLife:case SupplyKind.MageLife:return lives[i];default:return 0;
            }
        }
    }
}
