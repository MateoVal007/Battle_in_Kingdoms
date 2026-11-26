using UnityEngine;
using UnityEngine.InputSystem;

namespace Valeriano.CharacterPreview
{
    public sealed class MagePreview : MonoBehaviour
    {
        public Animator animator;
        public CharacterController motor;
        public Transform staff;

        PreviewJumpMotion jumpMotion;
        float castRemaining;
        string pose;
        int facing = 1;

        void Update()
        {
            var keyboard = Keyboard.current;
            float move = keyboard == null ? 0 :
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            bool jump = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            if (jump) castRemaining = 0;
            else if (keyboard != null && keyboard.fKey.wasPressedThisFrame && motor.isGrounded
                && jumpMotion.Current == PreviewJumpMotion.Phase.Ground && castRemaining <= 0)
                castRemaining = 1f;

            castRemaining = Mathf.Max(0, castRemaining - Time.deltaTime);
            if (castRemaining > 0) move = 0;
            if (move != 0 && jumpMotion.Current == PreviewJumpMotion.Phase.Ground) facing = move > 0 ? 1 : -1;
            animator.transform.localRotation = Quaternion.Euler(0, facing * 137, 0);
            jumpMotion.Step(motor, move * .95f, jump, Time.deltaTime);
            string next = jumpMotion.Pose(castRemaining > 0 ? "Cast" : move != 0 ? "Walk" : "Idle");
            if (next == pose) return;
            pose = next;
            animator.CrossFadeInFixedTime(next, .055f);
        }

        void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 425, 66), "");
            GUI.Label(new Rect(28, 24, 400, 21), "MAGO · Prueba de rig y bastón");
            GUI.Label(new Rect(28, 49, 400, 22), "A/D caminar  ·  Espacio saltar  ·  F gesto de hechizo");
        }
    }
}

