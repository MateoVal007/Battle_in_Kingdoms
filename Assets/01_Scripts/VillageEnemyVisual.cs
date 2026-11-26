using UnityEngine;

public sealed class VillageEnemyVisual : MonoBehaviour
{
    public Animator animator;
    public const float FallDuration = 1.3f;
    int previousHealth;
    float previousAttack, holdUntil;
    Vector3 previousPosition;
    string state;
    bool initialized;

    public void Present(VillageEnemy enemy)
    {
        if (!initialized)
        {
            previousHealth = enemy.health;
            previousPosition = enemy.transform.position;
            initialized = true;
        }
        if(enemy.IsFrozen){previousHealth=enemy.health;previousAttack=0;previousPosition=enemy.transform.position;holdUntil=0;return;}
        float deltaX=enemy.transform.position.x-previousPosition.x;
        float movement = Mathf.Abs(deltaX);
        previousPosition = enemy.transform.position;
        if (enemy.health <= 0)
        {
            Play("Fall");
            previousHealth = enemy.health;
            return;
        }
        transform.localRotation = Quaternion.Euler(0, enemy.Facing > 0 ? 110 : -110, 0);
        if (enemy.health < previousHealth)
        {
            Play("Hit", true);
            holdUntil = Time.time + .46f;
        }
        else if (enemy.AttackVisual > 0 && previousAttack <= 0)
        {
            Play("Attack", true);
            animator.speed = .433333f / Mathf.Max(.1f, enemy.windup);
            holdUntil = Time.time + 1.3f / animator.speed;
        }
        else if (Time.time >= holdUntil)
            Play(movement > .0005f ? enemy.bomber&&deltaX*enemy.Facing<0?"WalkBack":"Walk" : "Idle");
        previousAttack = enemy.AttackVisual;
        previousHealth = enemy.health;
    }

    void Play(string next, bool restart = false)
    {
        if (state == next && !restart) return;
        animator.speed = 1;
        if (state == null || next == "Fall") animator.Play(next, 0, 0);
        else animator.CrossFadeInFixedTime(next, .065f, 0, 0);
        state = next;
    }

    void OnEnable() { initialized = false; state = null; holdUntil = previousAttack = 0; }
}
