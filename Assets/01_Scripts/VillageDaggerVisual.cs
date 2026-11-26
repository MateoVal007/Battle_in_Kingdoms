using UnityEngine;

public sealed class VillageDaggerVisual : MonoBehaviour
{
    public Animator animator;
    public GameObject shield;
    public const float FallDuration = 1.3f;
    int previousHealth, previousShield;
    float previousAttack, holdUntil;
    Vector3 previousPosition;
    string state;
    bool initialized;

    public void Present(VillageEnemy enemy)
    {
        if (!initialized)
        {
            previousHealth=enemy.health;previousShield=enemy.shieldHealth;
            previousPosition=enemy.transform.position;initialized=true;
        }
        float movement=Mathf.Abs(enemy.transform.position.x-previousPosition.x);
        previousPosition=enemy.transform.position;
        transform.localRotation=Quaternion.Euler(0,enemy.Facing>0?-70:70,0);
        if(shield)shield.SetActive(enemy.shieldHealth>0);
        if(enemy.shieldHealth<previousShield&&enemy.shieldHealth>0)
        {Play("ShieldImpact",true);holdUntil=Time.time+.37f;}
        if(enemy.health<=0)Play("Fall");
        else if(enemy.health<previousHealth){Play("Hit",true);holdUntil=Time.time+.4f;}
        else if(enemy.AttackVisual>0&&previousAttack<=0)
        {Play("Throw",true);holdUntil=Time.time+enemy.windup+.22f;}
        else if(Time.time>=holdUntil)Play(movement>.0005f?"Walk":"Idle");
        previousAttack=enemy.AttackVisual;previousHealth=enemy.health;previousShield=enemy.shieldHealth;
    }
    void Play(string next,bool restart=false)
    {
        if(state==next&&!restart)return;
        if(state==null||next=="Fall")animator.Play(next,0,0);
        else animator.CrossFadeInFixedTime(next,.08f,0,0);
        state=next;
    }
    void OnEnable(){initialized=false;state=null;holdUntil=previousAttack=0;}
}

