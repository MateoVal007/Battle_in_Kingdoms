using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SwordSidePreview : MonoBehaviour
{
    public Animator animator;
    public float speed = 2.3f;
    int facing = 1;
    bool attacking;
    string state;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (attacking && animator.GetCurrentAnimatorStateInfo(0).IsName("SwordAttack")
            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
            attacking = false;

        float move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
            - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
        if (!attacking)
        {
            if (move != 0) facing = move > 0 ? 1 : -1;
            transform.rotation = Quaternion.Euler(0, facing * 90, 0);
            transform.position += Vector3.right * (move * speed * Time.deltaTime);
            if (keyboard.fKey.wasPressedThisFrame) attacking = true;
        }
        var position = transform.position;
        position.z = 0;
        transform.position = position;
        string next = attacking ? "SwordAttack" : move != 0 ? "Walk" : "Idle";
        if (next == state) return;
        animator.CrossFadeInFixedTime(next, .06f, 0, 0);
        state = next;
    }

    void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 380, 55), "Prueba lateral · espadazo rápido");
        GUI.Label(new Rect(28, 43, 350, 23), "A/D o flechas mover y orientar · F atacar");
    }

    void LateUpdate() => AlignAttack(animator, transform.forward);

    public static void AlignAttack(Animator model, Vector3 direction)
    {
        model.transform.localRotation = Quaternion.identity;
        var left = model.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var right = model.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Vector3 animatedForward = Vector3.Cross(left.position - right.position, Vector3.up);
        animatedForward = Vector3.ProjectOnPlane(animatedForward, Vector3.up);
        if (animatedForward.sqrMagnitude < .001f) return;
        if (Vector3.Dot(animatedForward, direction) < 0) animatedForward = -animatedForward;
        float correction = Vector3.SignedAngle(animatedForward, direction, Vector3.up);
        model.transform.rotation = Quaternion.AngleAxis(correction, Vector3.up) * model.transform.rotation;
    }
}
