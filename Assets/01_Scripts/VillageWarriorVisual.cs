using UnityEngine;

public sealed class VillageWarriorVisual : MonoBehaviour
{
    public Animator bare;
    public Animator equipped;
    public Animator libraryAttack;
    public Animator libraryBare;
    public float punchDuration = .32f;
    public float armorBreakDuration = .65f;
    public Vector3 joggingShieldNormal;
    float armorBreakUntil;
    public bool ArmorBreakPlaying => Time.time < armorBreakUntil;
    public float libraryAttackDuration = .48f;
    float libraryAttackUntil;
    int libraryAttackFacing = 1;
    Animator active;
    int attack;
    bool wasAirborne;
    float landUntil;
    int lastArmor;
    int lastHealth;
    string state;
    Renderer[] skins;
    MaterialPropertyBlock tint;
    bool tinted;
    Transform libraryShield;
    Quaternion shieldRestRotation;
    Vector3 shieldLocalNormal;

    internal void Present(VillageHero hero)
    {
        if (lastArmor > 0 && hero.armor == 0 && !hero.hasEquipment && !hero.IsDown)
            BeginArmorBreak();
        if (hero.IsDown || hero.hasEquipment) armorBreakUntil = 0;
        bool breaking = libraryAttack && !hero.IsDown && ArmorBreakPlaying;
        if (hero.IsDown || !hero.hasEquipment) libraryAttackUntil = 0;
        bool freshAttack = attack != hero.AttackSerial;
        if (libraryAttack && hero.hasEquipment && freshAttack && !hero.IsDown && !breaking)
        {
            libraryAttackFacing = hero.Facing < 0 ? -1 : 1;
            libraryAttackUntil = Time.time + libraryAttackDuration;
            libraryAttack.gameObject.SetActive(true);
            libraryAttack.Play("SwordAttack", 0, 0);
            libraryAttack.Update(0f);
        }
        bool showingPunch = libraryBare && !hero.hasEquipment && !hero.crouching && !hero.IsDown
            && hero.PresentationAttackAge < punchDuration && !breaking;
        if (showingPunch && freshAttack)
        { libraryBare.gameObject.SetActive(true); libraryBare.Play("PunchJab", 0, 0); libraryBare.Update(0); }
        bool jogging = libraryAttack && hero.hasEquipment && !hero.IsDown && !hero.PresentationAirborne
            && !hero.blocking && !hero.crouching && hero.PresentationSpeed > 3
            && hero.PresentationAttackAge >= libraryAttackDuration && !breaking;
        if (jogging && (!libraryAttack.gameObject.activeSelf || !libraryAttack.GetCurrentAnimatorStateInfo(0).IsName("JogEquipped")))
        { libraryAttack.gameObject.SetActive(true); libraryAttack.Play("JogEquipped", 0, 0); libraryAttack.Update(0); }
        bool showingLibraryAttack = breaking || jogging || (libraryAttack && hero.hasEquipment && !hero.IsDown
            && Time.time < libraryAttackUntil);
        if (libraryAttack) libraryAttack.gameObject.SetActive(showingLibraryAttack);
        if (libraryBare) libraryBare.gameObject.SetActive(showingPunch);
        SetLibraryGear(!breaking);
        var next = hero.hasEquipment ? equipped : bare;
        if (active != next)
        {
            active = next;
            bare.gameObject.SetActive(next == bare);
            equipped.gameObject.SetActive(next == equipped);
            state = null;
            lastArmor = hero.armor;
            lastHealth = hero.health;
        }

        bool armed = next == equipped;
        bool guarding = armed && hero.blocking && !hero.IsDown;
        bool blockImpact = guarding && !hero.crouching && hero.armor < lastArmor && hero.health == lastHealth;
        lastArmor = hero.armor;
        lastHealth = hero.health;
        float yaw = showingLibraryAttack || showingPunch ? (breaking || showingPunch || jogging ? hero.Facing : libraryAttackFacing) * 90
            : armed ? (hero.Facing < 0 ? -109f : 147f) : (hero.Facing < 0 ? -134f : 134f);
        // Reflect the approved right-facing guard across the gameplay X axis.
        // R(-yaw) * S(-X) == S(-X) * R(yaw); bones and rigid equipment keep their proportions.
        bool mirrorGuard=guarding&&!showingLibraryAttack&&!showingPunch&&hero.Facing<0;
        var scale=transform.localScale;scale.x=Mathf.Abs(scale.x)*(mirrorGuard?-1:1);transform.localScale=scale;
        if(mirrorGuard)yaw=-147f;
        transform.localRotation = Quaternion.Euler(0, yaw, 0);
        if (showingLibraryAttack) equipped.gameObject.SetActive(false);
        else if (armed) equipped.gameObject.SetActive(true);
        if (showingLibraryAttack || showingPunch) bare.gameObject.SetActive(false);
        else if (!armed) bare.gameObject.SetActive(true);
        bool newAttack = attack != hero.AttackSerial;
        attack = hero.AttackSerial;
        bool airborne = hero.PresentationAirborne;
        if (wasAirborne && !airborne) landUntil = Time.time + .20f;
        wasAirborne = airborne;
        string suffix = armed ? "Equipped" : "";
        string crouchIdle = armed ? "CrouchIdle" : "CrouchIdleBare";
        bool moving = hero.PresentationSpeed > .1f;
        string clip;
        if (hero.IsDown) clip = crouchIdle;
        else if (hero.PresentationAttackAge < (armed && libraryAttack ? libraryAttackDuration : .30f)) clip = armed ? "SwordSlash" : hero.crouching ? "CrouchPunch" : "Punch";
        else if (airborne) clip = "Jump" + suffix;
        else if (Time.time < landUntil) clip = "Land" + suffix;
        else if (guarding)
        {
            var info = active.GetCurrentAnimatorStateInfo(0);
            bool impactPlaying = state == "BlockImpact" && (!info.IsName("BlockImpact") || info.normalizedTime < 1f);
            clip = hero.crouching ? "GuardCrouch" : blockImpact || impactPlaying ? "BlockImpact" : "Guard";
        }
        else if (hero.crouching) clip = moving && !armed ? "CrouchWalkBare" : crouchIdle;
        else if (moving) clip = (hero.PresentationSpeed > 3f ? "Run" : "Walk") + suffix;
        else clip = "Ready" + suffix;

        if (!showingLibraryAttack && !showingPunch)
        {
        active.speed = hero.IsDown ? .65f : 1f;
        if (state != clip || (blockImpact && clip == "BlockImpact") ||
            (newAttack && (clip == "SwordSlash" || clip == "Punch" || clip == "CrouchPunch")))
        {
            if (state == null) active.Play(clip, 0, 0);
            else active.CrossFadeInFixedTime(clip, clip is "Guard" or "GuardCrouch" ? .13f : .065f, 0, 0);
            state = clip;
        }

        }
        else state = null;

        bool flash = hero.RecoveryRemaining > 0 && Mathf.FloorToInt(Time.time * 12) % 2 == 0;
        if (skins == null) skins = GetComponentsInChildren<Renderer>(true);
        if (flash || hero.IsDown)
        {
            tint ??= new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", hero.IsDown ? Color.gray : flash ? new Color(1, .55f, .5f) : Color.white);
            foreach (var skin in skins) skin.SetPropertyBlock(tint);
        }
        else if (tinted)
            foreach (var skin in skins) skin.SetPropertyBlock(null);
        tinted = flash || hero.IsDown;
    }

    void LateUpdate()
    {
        if (libraryAttack && libraryAttack.gameObject.activeSelf)
            AlignLibraryPose(transform.forward, libraryAttack.GetCurrentAnimatorStateInfo(0).IsName("JogEquipped"));
        if (libraryBare && libraryBare.gameObject.activeSelf)
            SwordSidePreview.AlignAttack(libraryBare, transform.forward);
    }

    public void AlignLibraryPose(Vector3 forward, bool jogging)
    {
        if (!libraryAttack) return;
        SwordSidePreview.AlignAttack(libraryAttack, forward);
        if (!libraryShield)
        {
            foreach (var renderer in libraryAttack.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("Escudo")) continue;
                libraryShield = renderer.transform; shieldRestRotation = libraryShield.localRotation;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var normal = joggingShieldNormal;
                if (normal.sqrMagnitude < .001f)
                { var size = mesh.bounds.size; normal = size.x < size.y && size.x < size.z ? Vector3.right : size.y < size.z ? Vector3.up : Vector3.forward; }
                shieldLocalNormal = normal.normalized; break;
            }
        }
        if (!libraryShield) return;
        if (!jogging) { libraryShield.localRotation = shieldRestRotation; return; }
        // Carry the rigid plate upright while the forearm follows the jog.
        var viewer = Camera.main ? Vector3.ProjectOnPlane(Camera.main.transform.position - libraryShield.position, Vector3.up).normalized : Vector3.back;
        var facing = (Vector3.ProjectOnPlane(forward, Vector3.up).normalized * .85f + viewer * .45f).normalized;
        var localUp = Vector3.ProjectOnPlane(Vector3.up, shieldLocalNormal);
        if (localUp.sqrMagnitude < .001f) localUp = Vector3.ProjectOnPlane(Vector3.forward, shieldLocalNormal);
        libraryShield.rotation = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(shieldLocalNormal, localUp));
    }

    public void BeginArmorBreak()
    {
        if (!libraryAttack || ArmorBreakPlaying) return;
        armorBreakUntil = Time.time + armorBreakDuration;
        libraryAttackUntil = 0;
        libraryAttack.gameObject.SetActive(true);
        libraryAttack.Play("ShieldBreak", 0, 0);
        libraryAttack.Update(0);
        SetLibraryGear(false);
    }

    public void SetLibraryGear(bool visible)
    {
        if (!libraryAttack) return;
        foreach (var mesh in libraryAttack.GetComponentsInChildren<MeshRenderer>(true))
            if (mesh.name.StartsWith("Espada") || mesh.name.StartsWith("Escudo")) mesh.gameObject.SetActive(visible);
    }
}
