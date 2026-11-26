using UnityEngine;

public sealed class VillageMageVisual : MonoBehaviour
{
    public Animator animator;
    string state;
    bool wasAirborne;
    float landUntil;
    int attack;
    Renderer[] skins;
    MaterialPropertyBlock tint;
    bool tinted;

    internal void Present(VillageHero hero)
    {
        transform.localRotation = Quaternion.Euler(0, hero.Facing < 0 ? -137f : 137f, 0);
        bool airborne = hero.PresentationAirborne;
        if (wasAirborne && !airborne) landUntil = Time.time + .23f;
        wasAirborne = airborne;
        bool newAttack = attack != hero.AttackSerial;
        attack = hero.AttackSerial;
        bool attacking = !hero.IsDown && hero.PresentationAttackAge < .36f;

        string clip;
        if (hero.IsDown) clip = "CrouchIdle";
        else if (attacking) clip = airborne ? "AirCast" : hero.crouching ? "CrouchCast" : "Cast";
        else if (airborne) clip = "Jump";
        else if (Time.time < landUntil) clip = "Land";
        else if (hero.crouching)
        {
            bool inCrouch = state == "CrouchIdle" || state == "CrouchCast";
            var info = animator.GetCurrentAnimatorStateInfo(0);
            clip = inCrouch || (state == "CrouchDown" && info.IsName("CrouchDown") && info.normalizedTime >= 1)
                ? "CrouchIdle" : "CrouchDown";
        }
        else clip = hero.PresentationSpeed > .1f ? "Walk" : "Idle";

        if (state != clip || (attacking && newAttack))
        {
            if (state == null) animator.Play(clip, 0, 0);
            else animator.CrossFadeInFixedTime(clip, attacking ? .04f : .065f, 0, 0);
            state = clip;
        }

        bool flash = hero.RecoveryRemaining > 0 && Mathf.FloorToInt(Time.time * 12) % 2 == 0;
        if (flash || hero.IsDown || tinted)
        {
            skins ??= GetComponentsInChildren<Renderer>(true);
            tint ??= new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", hero.IsDown ? Color.gray : flash ? new Color(1, .55f, .5f) : Color.white);
            foreach (var skin in skins) skin.SetPropertyBlock(tint);
        }
        tinted = flash || hero.IsDown;
    }
}
