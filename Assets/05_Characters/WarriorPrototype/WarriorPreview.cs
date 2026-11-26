using UnityEngine;
using UnityEngine.InputSystem;

namespace Valeriano.CharacterPreview
{
    public sealed class WarriorPreview : MonoBehaviour
    {
        public Animator animator;
        public Animator armoredAnimator;
        public Transform visual;
        public CharacterController motor;
        public Camera view;
        public bool Equipped { get; private set; }
        public Animator ActiveAnimator => Equipped ? armoredAnimator : animator;

        public const float FacingYaw = 134f;

        PreviewJumpMotion jumpMotion;
        bool readyStance = true;
        int direction = 1;
        string currentAnimation;
        string action;
        bool wasCrouching;

        void Start()
        {
            Equip(false);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (action != null && Finished(action)) action = null;
            bool onGround = jumpMotion.Current == PreviewJumpMotion.Phase.Ground && motor.isGrounded;
            bool running = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            if (action == null && onGround)
            {
                if (keyboard.qKey.wasPressedThisFrame) readyStance = !readyStance;
                if (keyboard.eKey.wasPressedThisFrame) Equip(!Equipped, horizontal != 0, running);
            }

            bool blocking = Equipped && keyboard.gKey.isPressed;
            bool crouch = keyboard.sKey.isPressed;
            if (action == "BlockImpact" && (!blocking || crouch)) action = null;
            bool airborne = jumpMotion.Current == PreviewJumpMotion.Phase.Air;
            bool crouchTravel = crouch && !Equipped && horizontal != 0 && !airborne;
            if (action == null && horizontal != 0 && (!crouch || crouchTravel || airborne))
                direction = horizontal > 0 ? 1 : -1;
            // The sculpt's body already turns toward its sword. Calibrate each
            // side and keep that angle through the stop, crouch and guard.
            float yaw = Equipped ? (direction > 0 ? 147f : -109f) : direction * FacingYaw;
            visual.localRotation = Quaternion.RotateTowards(visual.localRotation,
                Quaternion.Euler(0, yaw, 0), 1080f * Time.deltaTime);

            if (onGround && keyboard.hKey.wasPressedThisFrame
                && (action == null || action == "Punch" || action == "SwordSlash"))
            {
                if (!blocking || !crouch)
                    action = blocking ? "BlockImpact" : Equipped ? "HitEquipped" : "HitBare";
            }
            else if (onGround && action == null && !blocking && (!crouch || !Equipped) && keyboard.fKey.wasPressedThisFrame)
                action = Equipped ? "SwordSlash" : crouch ? "CrouchPunch" : "Punch";

            // Match the travel of the planted foot in the authored cycles.
            float speed = running ? (Equipped ? 1.96f : 2.35f) : .97f;
            if (airborne) speed = Mathf.Max(speed, 2.35f);
            else if (crouch) speed = crouchTravel ? .82f : 0f;
            if (action != null || blocking && !airborne) speed = 0;
            bool jump = onGround && action == null && !blocking && !crouch && keyboard.spaceKey.wasPressedThisFrame;
            jumpMotion.Step(motor, horizontal * speed, jump, Time.deltaTime);

            string groundPose = action ?? GroundPose(crouch, blocking, horizontal != 0, running);
            string pose = jumpMotion.Pose(groundPose);
            if (Equipped && pose != groundPose) pose += "Equipped";
            Show(pose);
            wasCrouching = crouch;
        }

        string GroundPose(bool crouch, bool blocking, bool moving, bool running)
        {
            if (blocking) return crouch ? "GuardCrouch" : "Guard";
            if (crouch)
            {
                string down = Equipped ? "CrouchDown" : "CrouchDownBare";
                if (!wasCrouching || (currentAnimation == down && !Finished(down)))
                    return down;
                if (Equipped) return "CrouchIdle";
                return moving ? "CrouchWalkBare" : "CrouchIdleBare";
            }
            return TravelPose(moving, running);
        }

        string TravelPose(bool moving, bool running)
        {
            string state = moving ? (running ? "Run" : "Walk") : (readyStance ? "Ready" : "Idle");
            return Equipped ? state + "Equipped" : state;
        }

        bool Finished(string state)
        {
            var info = ActiveAnimator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(state) && info.normalizedTime >= 1f;
        }

        void LateUpdate()
        {
            Vector3 anchor = new Vector3(transform.position.x, 1.25f, 0);
            Vector3 cameraPosition = anchor + new Vector3(0, .25f, -5.6f);
            view.transform.position = Vector3.Lerp(view.transform.position, cameraPosition, 1 - Mathf.Exp(-8 * Time.deltaTime));
            view.transform.LookAt(anchor);
        }

        void Equip(bool equipped, bool moving = false, bool running = false)
        {
            float cycle = 0;
            if (moving && ActiveAnimator.gameObject.activeInHierarchy)
            {
                var info = ActiveAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(TravelPose(true, running))) cycle = Mathf.Repeat(info.normalizedTime, 1);
            }
            Equipped = equipped;
            animator.gameObject.SetActive(!equipped);
            armoredAnimator.gameObject.SetActive(equipped);
            wasCrouching = false;
            currentAnimation = TravelPose(moving, running);
            ActiveAnimator.Play(currentAnimation, 0, cycle);
            ActiveAnimator.Update(0);
        }

        void Show(string state)
        {
            if (currentAnimation == state) return;
            bool guarding = state is "Guard" or "GuardCrouch" || currentAnimation is "Guard" or "GuardCrouch" or "BlockImpact";
            currentAnimation = state;
            float blend = state.StartsWith("Prepare") ? .04f : guarding ? .13f : .075f;
            ActiveAnimator.CrossFadeInFixedTime(state, blend);
        }

        void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 580, 94), "");
            GUI.Label(new Rect(28, 24, 500, 21), "GUERRERO · " + (Equipped ? "Espada, escudo y armadura" : "Sin equipo"));
            GUI.Label(new Rect(28, 49, 530, 22), "A/D caminar  ·  Mayús correr  ·  Espacio saltar  ·  E equipo  ·  Q postura");
            GUI.Label(new Rect(28, 72, 530, 22), Equipped ? "F tajo  ·  S agacharse  ·  G cubrirse  ·  S + G guardia baja  ·  G + H impacto" : "F puñetazo  ·  S agacharse  ·  S + F golpe bajo  ·  H impacto");
        }
    }
}
