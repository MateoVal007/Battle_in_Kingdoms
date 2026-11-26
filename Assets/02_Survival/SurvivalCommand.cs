using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kingdoms.Survival
{
    public struct SurvivalCommand : INetworkInput
    {
        public Vector3 Destination,Aim;
        public Vector2 Move;
        public int ClickSerial,CycleSerial,UseSerial,LifeSerial,JumpSerial;
        public NetworkBool Attack,Guard,HasAim;
    }
}
