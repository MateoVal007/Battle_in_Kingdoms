using UnityEngine;

public sealed class VillageArcherVisual : MonoBehaviour
{
    public Animator animator;
    public Transform muzzle;
    int previousHealth;
    float previousAttack, heldUntil;
    string state;
    bool initialized;

    public void Present(VillageEnemy enemy)
    {
        transform.localRotation = Quaternion.Euler(0, enemy.Facing * 90 - 23, 0);
        if (!initialized) { previousHealth = enemy.health; initialized = true; }
        if (enemy.IsFrozen) { previousAttack = 0; previousHealth = enemy.health; return; }
        string next = null;
        if (enemy.health <= 0) next = "Fall";
        else if (enemy.health < previousHealth) { next = "Hit"; heldUntil = Time.time + .5f; }
        else if (enemy.AttackVisual < 0 && previousAttack >= 0) { next = "Shoot"; heldUntil = Time.time + .6f; }
        else if (Time.time >= heldUntil) next = "Idle";
        if (next != null && next != state)
        { if (state == null || next == "Fall") animator.Play(next, 0, 0); else animator.CrossFadeInFixedTime(next, .05f, 0, 0); state = next; }
        previousHealth = enemy.health; previousAttack = enemy.AttackVisual;
    }
    void OnEnable() { initialized = false; state = null; heldUntil = previousAttack = 0; }
}
