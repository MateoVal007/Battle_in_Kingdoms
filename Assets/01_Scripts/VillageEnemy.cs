using UnityEngine;

// Only archers remain at their post; ground enemies pursue living players.
public class VillageEnemy : MonoBehaviour
{
 public int health=60;
 public float originX;
 public bool ranged,whip,daggerThrower,goblin,bomber,bomberAlt;
 public int shieldHealth;
 Transform projectileShield;
 public float detectionRange=14,moveSpeed=2.3f,attackRange=1.8f,windup=.45f,cooldown=1.3f;
 public int damage=15;
 public float patrolRadius=2.5f,leashRadius=11;
 public string State {get;private set;}="Patrulla";
 public int HomeReturns {get;private set;}
 Vector3 home;bool returning;float patrolDirection=1,pauseUntil;
 public float Facing {get;private set;}=-1;
 public float AttackVisual {get;private set;}
 public int AttackCount {get;private set;}
 public VillageHero Target {get;private set;}
 VillageEnemyVisual animatedVisual;
 VillageDaggerVisual daggerVisual;
 VillageArcherVisual archerVisual;
 float deathUntil;
 CharacterController motor; Renderer body; MaterialPropertyBlock tint; LineRenderer weapon;
 float nextHit,frozenUntil,frozenX,attackAt,vertical; Vector3 aim; bool preparing;
 FrozenEnemyVisual freezeVisual;
 public const float FreezeDuration=3f;
 public bool CanFreeze=>!whip&&(!daggerThrower||shieldHealth<=0);
 public bool IsFrozen=>health>0&&CanFreeze&&Time.time<frozenUntil;
 public float FrozenRemaining=>IsFrozen?Mathf.Max(0,frozenUntil-Time.time):0;
 float HalfHeight=>whip?1.5f:1;
 void Start()
 {
  home=transform.position;originX=home.x;body=GetComponent<Renderer>();tint=new MaterialPropertyBlock();
  var old=GetComponent<Collider>();if(old)old.enabled=ranged;
  motor=gameObject.AddComponent<CharacterController>();motor.height=goblin?1.9f:2;motor.radius=.42f;if(goblin)motor.center=Vector3.down*.05f;
  motor.skinWidth=.03f;motor.stepOffset=.3f;motor.slopeLimit=50;if(ranged)motor.enabled=false;
  var o=new GameObject(ranged?"Arco":"Alcance del látigo");o.transform.SetParent(transform,false);
  weapon=o.AddComponent<LineRenderer>();weapon.useWorldSpace=true;weapon.positionCount=whip?12:9;
  weapon.widthMultiplier=whip?.09f:.055f;weapon.sharedMaterial=new Material(body.sharedMaterial);
  if(!ranged&&!whip)weapon.enabled=false;
  if (daggerThrower)
  {
   var prefab=Resources.Load<VillageDaggerVisual>("VillageDaggerVisual");
   if(prefab){daggerVisual=Instantiate(prefab,transform,false);daggerVisual.transform.localPosition=Vector3.down;body.enabled=false;}
   else CreateProjectileShield();
  }
  if (!ranged && !whip && !daggerThrower)
  {
   var prefab = Resources.Load<GameObject>(bomber?(bomberAlt?"VillageBomber1Visual":"VillageBomberVisual"):goblin?"VillageGoblinVisual":"VillageEnemy1Visual");
   var model = prefab ? prefab.GetComponent<VillageEnemyVisual>() : null;
   if (model)
   {
    animatedVisual = Instantiate(model, transform, false);
    animatedVisual.transform.localPosition = Vector3.down;
    body.enabled = false;
   }
  }
  if(ranged)
  {
   var prefab=Resources.Load<VillageArcherVisual>("VillageArcherVisual");
   if(prefab){archerVisual=Instantiate(prefab,transform,false);archerVisual.transform.localPosition=Vector3.down;body.enabled=false;weapon.enabled=false;}
  }
  freezeVisual=gameObject.AddComponent<FrozenEnemyVisual>();
 }
 void CreateProjectileShield()
 {
  var shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
  shield.name = "Escudo de dagas";
  var collider = shield.GetComponent<Collider>();
  collider.enabled = false;
  Destroy(collider);
  projectileShield = shield.transform;
  projectileShield.SetParent(transform, false);
  projectileShield.localScale = new Vector3(.28f, 1.27f, .88f);
  shield.GetComponent<Renderer>().material.color = new Color(.38f, .62f, .69f);
  projectileShield.gameObject.SetActive(shieldHealth > 0);
 }
 void Update()
 {
  if(!VillageOnline.CanSimulate)return;
  if(health<=0){if(deathUntil>0&&Time.time>=deathUntil)gameObject.SetActive(false);return;}
  if(IsFrozen){State="Congelado";AttackVisual=0;Fall();var at=transform.position;at.x=frozenX;transform.position=at;return;}
  if(!ranged&&!returning&&(Mathf.Abs(transform.position.x-home.x)>leashRadius||(Target&&Mathf.Abs(Target.transform.position.x-home.x)>leashRadius+2)))
  {returning=true;Target=null;preparing=false;AttackVisual=0;State="Regreso";}
  if(returning)
  {
   float dx=home.x-transform.position.x;
   if(Mathf.Abs(dx)<.2f){returning=false;HomeReturns++;pauseUntil=Time.time+.7f;State="Patrulla";}
   else {Facing=Mathf.Sign(dx);Move(Facing);}
  }
  else
  {
   if(!Target||Target.IsDown||(ranged&&!Below(Target))||Vector3.Distance(Target.transform.position,transform.position)>detectionRange+3)
   {
    Target=null;float best=detectionRange;
    foreach(var h in FindObjectsByType<VillageHero>())
    {float d=Vector3.Distance(h.transform.position,transform.position);if(!h.IsDown&&(!ranged||Below(h))&&d<best){best=d;Target=h;}}
    if(!Target&&preparing){preparing=false;AttackVisual=0;}
    if(!ranged&&!Target&&Mathf.Abs(transform.position.x-home.x)>patrolRadius+.3f){returning=true;State="Regreso";}
   }
   if(preparing)
   {
    if(bomber&&Target&&Mathf.Abs(Target.transform.position.x-transform.position.x)<5)Move(-Facing,.9f);
    State="Preparación";AttackVisual=Mathf.Clamp01(1-(attackAt-Time.time)/windup);
    if(Time.time>=attackAt){preparing=false;AttackVisual=-1;if(!ranged||(Target&&!Target.IsDown&&Below(Target))){AttackCount++;Strike();}nextHit=Time.time+cooldown;}
   }
   else
   {
    AttackVisual=Time.time<nextHit-cooldown+.23f?-1:0;
    if(Target)
    {
     State=Time.time<nextHit?"Recuperación":"Persecución";Vector3 offset=Target.transform.position-transform.position;
     if(Mathf.Abs(offset.x)>.1f)Facing=Mathf.Sign(offset.x);
     float range=ranged?18:bomber?11:attackRange;
     if(offset.magnitude<=range&&Time.time>=nextHit&&Clear(transform.position+Vector3.up*.3f,Target.transform.position))
     {preparing=true;attackAt=Time.time+windup;aim=Target.transform.position+Vector3.up*.15f;AttackVisual=.01f;}
     else if(bomber){if(Mathf.Abs(offset.x)<5)Move(-Facing,.9f);else if(Mathf.Abs(offset.x)>8)Move(Facing);}
     else if(!ranged&&Mathf.Abs(offset.x)>attackRange*.8f&&Time.time>=nextHit-.5f)Move(Facing);
    }
    else if(!ranged)
    {
     State="Patrulla";
     if(Time.time>=pauseUntil)
     {
      Facing=patrolDirection;
      if((transform.position.x-home.x)*patrolDirection>=patrolRadius||!Move(patrolDirection,.6f))
      {patrolDirection=-patrolDirection;pauseUntil=Time.time+.65f;}
     }
    }
   }
  }
  Fall();
 }
 void Fall(){if(!ranged&&motor&&motor.enabled){vertical=motor.isGrounded?-2:Mathf.Max(-20,vertical-25*Time.deltaTime);motor.Move(Vector3.up*(vertical*Time.deltaTime));}}
 bool Move(float direction,float rate=1)
 {
  float probe=transform.position.x+direction*(whip?.95f:.7f),feet=transform.position.y-HalfHeight;
  bool floor=false;
  foreach(var hit in Physics.RaycastAll(new Vector3(probe,feet+.5f,0),Vector3.down,1.15f,~0,QueryTriggerInteraction.Ignore))
   if(!hit.collider.GetComponentInParent<VillageEnemy>()&&!hit.collider.GetComponentInParent<VillageHero>()&&hit.normal.y>.55f){floor=true;break;}
  if(!floor)return false;
  float before=transform.position.x;motor.Move(Vector3.right*(direction*moveSpeed*rate*Time.deltaTime));
  return Mathf.Abs(transform.position.x-before)>Mathf.Max(.00001f,moveSpeed*rate*Time.deltaTime*.1f);
 }
 bool Below(VillageHero h)=>h.transform.position.y<transform.position.y-1&&Mathf.Abs(h.transform.position.x-transform.position.x)<8;
 bool Clear(Vector3 from,Vector3 to)
 {
  foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore))
   if(!hit.collider.GetComponentInParent<VillageHero>()&&!hit.collider.GetComponentInParent<VillageEnemy>())return false;
  return true;
 }
 void Strike()
 {
  if(bomber)
  {
   var muzzle=transform.position+new Vector3(Facing*.6f,.35f,0);
   VillageEnemyBolt.CreateBomb(this,muzzle,aim-Vector3.up*.85f);
   return;
  }
  if (ranged || daggerThrower)
  {
   var muzzle = transform.position + new Vector3(Facing * .7f, daggerThrower ? .15f : -.15f, 0);
   if(ranged&&archerVisual&&archerVisual.muzzle){muzzle=archerVisual.muzzle.position;muzzle.z=0;}
   if (ranged && aim.y >= muzzle.y - .5f) return;
   if (!Clear(muzzle, aim)) return;
   var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
   projectile.name = daggerThrower ? "Daga del guardia" : "Flecha del arquero";
   projectile.transform.position = muzzle;
   var collider = projectile.GetComponent<Collider>();
   collider.enabled = false;
   Destroy(collider);
   projectile.GetComponent<Renderer>().material.color = daggerThrower
       ? new Color(.78f, .84f, .92f) : new Color(.42f, .2f, .06f);
   var bolt = projectile.AddComponent<VillageEnemyBolt>();
   bolt.owner = this;
   bolt.arrow = ranged;
   bolt.dagger = daggerThrower;
   bolt.velocity = (aim - muzzle).normalized * (daggerThrower ? 12 : 9);
   return;
  }
  foreach(var h in FindObjectsByType<VillageHero>())
  {
   Vector3 d=h.transform.position-transform.position;
   if(!h.IsDown&&d.x*Facing>=-.2f&&Mathf.Abs(d.x)<=attackRange&&Mathf.Abs(d.y)<(whip?1.65f:1.1f)
    &&Clear(transform.position+Vector3.up*.15f,h.transform.position))h.Damage(damage,true,transform.position.x,h.transform.position.y);
  }
 }
 void LateUpdate()
 {
  if((animatedVisual||daggerVisual||archerVisual)&&health<=0&&motor&&motor.enabled)motor.enabled=false;
  if(animatedVisual)animatedVisual.Present(this);
  if(daggerVisual)daggerVisual.Present(this);
  if(archerVisual)archerVisual.Present(this);
  if(!body){freezeVisual?.Present(IsFrozen);return;}
  Color baseColor = daggerThrower ? new Color(.24f, .43f, .38f)
      : whip ? new Color(.5f, .12f, .2f) : new Color(.72f, .18f, .12f);
  tint.SetColor("_BaseColor", AttackVisual > 0 ? Color.yellow : baseColor);
  body.SetPropertyBlock(tint);
  if (projectileShield)
  {
   projectileShield.gameObject.SetActive(shieldHealth > 0);
   projectileShield.localPosition = new Vector3(Facing * .48f, 0, -.33f);
   projectileShield.localRotation = Quaternion.Euler(0, Facing * 19f, 0);
  }
  if(ranged)for(int i=0;i<9;i++){float a=Mathf.Lerp(-1.35f,1.35f,i/8f);weapon.SetPosition(i,transform.position+new Vector3(Facing*(.45f+Mathf.Cos(a)*.35f),.35f+Mathf.Sin(a)*.7f,-.5f));}
  if(whip)
  {
   weapon.enabled=AttackVisual!=0;weapon.sharedMaterial.color=AttackVisual>0?new Color(1,.7f,.15f):new Color(1,.25f,.08f);
   for(int i=0;i<12;i++){float t=i/11f;weapon.SetPosition(i,transform.position+new Vector3(Facing*(.6f+t*(attackRange-.6f)),.1f+Mathf.Sin(t*Mathf.PI*2)*.13f,-.7f));}
  }
  freezeVisual?.Present(IsFrozen);
 }
 void OnControllerColliderHit(ControllerColliderHit hit)
 {
  var hero=hit.collider.GetComponentInParent<VillageHero>();if(!hero||health<=0)return;
  bool heroAbove=hero.transform.position.y>transform.position.y+.65f;
  hero.Damage(damage,!heroAbove,transform.position.x,transform.position.y);
 }
 public void SetHome(Vector3 position)
 {if(motor)motor.enabled=false;transform.position=position;home=position;originX=position.x;frozenUntil=0;Target=null;returning=preparing=false;AttackVisual=0;pauseUntil=nextHit=0;HomeReturns=0;if(motor&&!ranged)motor.enabled=true;Physics.SyncTransforms();}
 public void ApplyRemote(float facing,float action,float frozen=0){Facing=facing;AttackVisual=action;frozenUntil=Time.time+Mathf.Max(0,frozen);}
 public void HitProjectile(int amount, bool ice, float direction)
 {
  if (!VillageOnline.CanSimulate || health <= 0) return;
  if (daggerThrower && shieldHealth > 0 && direction * Facing < 0)
  {
   shieldHealth = Mathf.Max(0, shieldHealth - amount);
   if (shieldHealth == 0)
   {
    var game = FindAnyObjectByType<VillageGame>();
    if (game) game.notice = "Escudo del guardia destruido";
   }
   return;
  }
  Hit(amount, ice);
 }

 public void Hit(int amount,bool ice)
 {
  if(!VillageOnline.CanSimulate||health<=0)return;
  health=Mathf.Max(0,health-amount);
  if(ice&&health>0&&CanFreeze){frozenUntil=Time.time+FreezeDuration;frozenX=transform.position.x;preparing=false;AttackVisual=0;nextHit=Mathf.Max(nextHit,frozenUntil+.15f);State="Congelado";}
  if(health==0)
  {
   preparing=false;AttackVisual=0;
   if(animatedVisual||daggerVisual||archerVisual){deathUntil=Time.time+VillageEnemyVisual.FallDuration;if(motor)motor.enabled=false;}
   else gameObject.SetActive(false);
  }
 }
}

