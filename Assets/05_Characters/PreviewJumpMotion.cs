using UnityEngine;

namespace Valeriano.CharacterPreview
{
    public struct PreviewJumpMotion
    {
        public enum Phase { Ground, Prepare, Air, Land }

        const float Takeoff = 6.4f;
        const float Gravity = 20f;
        float vertical;
        float remaining;
        public Phase Current { get; private set; }

        public void Step(CharacterController motor, float speed, bool jump, float dt)
        {
            if (Current == Phase.Land)
            {
                remaining -= dt;
                if (remaining <= 0) Current = Phase.Ground;
            }
            if (Current == Phase.Ground && motor.isGrounded && jump)
            {
                Current = Phase.Prepare;
                remaining = .125f;
            }
            if (Current == Phase.Prepare)
            {
                remaining -= dt;
                if (remaining <= 0)
                {
                    Current = Phase.Air;
                    vertical = Takeoff;
                }
            }

            if (Current == Phase.Air) vertical -= Gravity * dt;
            else vertical = -2f;

            motor.Move(new Vector3(speed, vertical, 0) * dt);
            if (Current == Phase.Air && motor.isGrounded && vertical < 0)
            {
                Current = Phase.Land;
                remaining = .21f;
            }
            else if (Current == Phase.Ground && !motor.isGrounded)
                Current = Phase.Air;
        }

        public string Pose(string groundPose)
        {
            switch (Current)
            {
                case Phase.Prepare: return "Prepare";
                case Phase.Air: return "Jump";
                case Phase.Land: return "Land";
                default: return groundPose;
            }
        }
    }
}

