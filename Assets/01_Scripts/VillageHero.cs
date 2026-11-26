using UnityEngine;
using UnityEngine.InputSystem;

public class VillageHero : MonoBehaviour
{
    public bool isMage;
    public VillageGame game;
    public int health = 100, armor;
    public int stars,extraLives;
    public const float RecoveryDuration=3;
    public Material protectionMaterial;
    Transform protectionBubble;
    public bool ProtectionVisible=>protectionBubble&&protectionBubble.gameObject.activeSelf;
    public float RecoveryRemaining=>Mathf.Max(0,immuneUntil-Time.time);
    public bool hasEquipment;
    public string enchantment = "Ninguno";
    public bool blocking, casting;
    public bool crouching, shieldUp, itemActive;
    public int selectedItem;
    public float fireCharge, iceCharge, protectionCharge;
    public const float ItemCapacity = 25;
    public const int SpellCapacity=12;
    public static float ItemMaximum(int index)=>index==2?ItemCapacity:SpellCapacity;
    public static readonly string[] ItemNames = { "Fuego", "Hielo", "Protección" };
    Transform postureVisual;
    Transform fist;
    public float immuneUntil;
    public Transform sword, shield, arc;
    public GameObject armorVisual;
    public float speed = 6, jumpSpeed = 11, gravity = 26;

    CharacterController motor;
    Renderer body;
    MaterialPropertyBlock tint;
    Color bodyColor;
    float vertical, facing = 1, nextAttack, groundedAt = -10, jumpPressedAt = -10;
    int combo;
    float lastAttack = -10;
    VillageWarriorVisual warriorVisual;
    VillageMageVisual mageVisual;
    public float PresentationSpeed { get; private set; }
    public bool PresentationAirborne { get; private set; }
    internal float PresentationAttackAge => Time.time - lastAttack;

    public bool IsDown => health <= 0;
    public float VerticalSpeed => vertical;
    public bool Grounded => motor && motor.isGrounded;
    public float Facing => facing;
    public int AttackSerial { get; private set; }

    void Awake()
    {
        motor = GetComponent<CharacterController>();
        body = GetComponent<Renderer>();
        bodyColor = body.sharedMaterial.color;
        tint = new MaterialPropertyBlock();
        {
            // Move only presentation into a pivot; collision dimensions remain independent.
            postureVisual=new GameObject("Postura visual").transform;postureVisual.SetParent(transform,false);
            var children=new System.Collections.Generic.List<Transform>();
            foreach(Transform child in transform)if(child!=postureVisual)children.Add(child);
            foreach(var child in children)child.SetParent(postureVisual,true);
            var torso=new GameObject("Cuerpo visual");torso.transform.SetParent(postureVisual,false);
            torso.AddComponent<MeshFilter>().sharedMesh=GetComponent<MeshFilter>().sharedMesh;
            var renderer=torso.AddComponent<MeshRenderer>();renderer.sharedMaterial=body.sharedMaterial;body.enabled=false;body=renderer;
            if(!isMage){var hand=GameObject.CreatePrimitive(PrimitiveType.Sphere);hand.name="Puño sin equipo";Destroy(hand.GetComponent<Collider>());hand.transform.SetParent(postureVisual,false);hand.transform.localScale=Vector3.one*.35f;hand.GetComponent<Renderer>().material.color=new Color(.85f,.68f,.49f);fist=hand.transform;}
        }
        if(isMage)
        {
            var bubble=GameObject.CreatePrimitive(PrimitiveType.Sphere);bubble.name="Campo de protección";var c=bubble.GetComponent<Collider>();c.enabled=false;Destroy(c);
            bubble.transform.SetParent(transform,false);bubble.transform.localScale=Vector3.one*2.9f;bubble.GetComponent<Renderer>().sharedMaterial=protectionMaterial?protectionMaterial:body.sharedMaterial;
            bubble.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;protectionBubble=bubble.transform;
            var ring=new GameObject("Borde circular del campo");ring.transform.SetParent(bubble.transform,false);var line=ring.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=.018f;line.sharedMaterial=body.sharedMaterial;
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,-.12f));}
        }
        if (!isMage)
        {
            var prefab = Resources.Load<GameObject>("VillageWarriorVisual");
            if (prefab && prefab.TryGetComponent<VillageWarriorVisual>(out var model))
            {
                warriorVisual = Instantiate(model, transform, false);
                warriorVisual.transform.localPosition = Vector3.down;
                warriorVisual.transform.localScale = Vector3.one * (2f / 1.8f);
                HidePlaceholder();
            }
        }
        else
        {
            var prefab = Resources.Load<GameObject>("VillageMageVisual");
            if (prefab && prefab.TryGetComponent<VillageMageVisual>(out var model))
            {
                mageVisual = Instantiate(model, transform, false);
                mageVisual.transform.localPosition = Vector3.down;
                mageVisual.transform.localScale = Vector3.one * (2f / 1.8f);
                HidePlaceholder();
            }
        }
        UpdatePresentation();
    }
    void HidePlaceholder()
    {
        body.enabled = false;
        foreach (Transform part in postureVisual)
            if (part.name == "Cabeza" || (isMage && (part.name == "Baston" || part.name == "Cristal baston"))) part.gameObject.SetActive(false);
    }
    void LateUpdate(){UpdateProtection();if(warriorVisual)warriorVisual.Present(this);if(mageVisual)mageVisual.Present(this);}
    void UpdateProtection(){if(protectionBubble)protectionBubble.gameObject.SetActive(isMage&&!IsDown&&itemActive&&selectedItem==2&&enchantment=="Protección");}
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var enemy=hit.collider.GetComponentInParent<VillageEnemy>();if(!enemy||enemy.health<=0)return;
        bool landing=hit.normal.y>.4f&&vertical<=0;
        Damage(enemy.damage,!landing,enemy.transform.position.x,enemy.transform.position.y);
    }
    public void AddStars(int amount)
    {
        if(!VillageOnline.CanSimulate||IsDown||amount<=0)return;
        stars+=amount;int gained=stars/100;stars%=100;extraLives+=gained;
        if(gained>0&&game)game.notice=(isMage?"Mago":"Guerrero")+": +"+gained+" vida extra · 100 estrellas";
    }
    public bool TryUseExtraLife()
    {
        if(!VillageOnline.CanSimulate||!IsDown||extraLives<=0||!game)return false;
        extraLives--;Revive(game.SafeSpawn(game.checkpoint+(isMage?2:0)),100);game.notice=(isMage?"Mago":"Guerrero")+": vida extra utilizada";return true;
    }

    public void Teleport(Vector3 position)
    {
        if (!motor) motor = GetComponent<CharacterController>();
        motor.enabled = false;
        transform.position = position;
        motor.enabled = true;
        vertical = 0;
        groundedAt = jumpPressedAt = -10;
        PresentationSpeed = 0;
        PresentationAirborne = false;
    }

    public CollisionFlags MoveInLift(Vector2 movement)
    {
        vertical = 0;
        groundedAt = jumpPressedAt = -10;
        return motor.Move(movement);
    }

    void Update()
    {
        if (VillageOnline.Instance) return;
        var k = Keyboard.current;
        if (k == null || game == null) return;
        if(isMage?k.yKey.wasPressedThisFrame:k.tKey.wasPressedThisFrame)TryUseExtraLife();
        if (game.ActiveHero != this)
        {
            Simulate(0, false, false, false, Time.deltaTime);
            return;
        }
        Simulate(isMage ? (k.rightArrowKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed ? 1 : 0) : (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
            isMage ? k.upArrowKey.wasPressedThisFrame : k.spaceKey.wasPressedThisFrame || (k.wKey.wasPressedThisFrame&&!k.lKey.isPressed),
            isMage ? k.kKey.wasPressedThisFrame : k.fKey.wasPressedThisFrame,
            !isMage && k.lKey.isPressed, Time.deltaTime,
            (isMage?k.downArrowKey.isPressed:k.sKey.isPressed),!isMage&&(k.wKey.isPressed||k.upArrowKey.isPressed),
            isMage?(k.eKey.wasPressedThisFrame?1:0)-(k.qKey.wasPressedThisFrame?1:0):0,isMage&&k.rKey.wasPressedThisFrame,
            !isMage&&(k.leftShiftKey.isPressed||k.rightShiftKey.isPressed));
    }

    public void Simulate(float move, bool jumpPressed, bool attack, bool ability, float dt, bool crouch=false, bool aimUp=false, int cycleItem=0, bool useItem=false, bool walk=false)
    {
        PresentationSpeed = 0;
        if (IsDown)
        {
            PresentationAirborne = false;
            blocking = casting = false;
            if (arc) arc.gameObject.SetActive(false);
            if (motor.isGrounded && vertical < 0) vertical = -2;
            vertical -= gravity * dt;
            motor.Move(Vector3.up * vertical * dt);
            tint.SetColor("_BaseColor", Color.gray); body.SetPropertyBlock(tint);UpdateProtection();
            return;
        }
        if (game && game.Lift && game.Lift.Carrying)
        {
            PresentationAirborne = false;
            blocking = casting = shieldUp = false;
            SetPosture(false);
            if (isMage) TickItem(dt);
            UpdatePresentation();
            return;
        }
        blocking = !isMage && hasEquipment && ability;
        SetPosture(crouch&&motor.isGrounded);
        shieldUp = blocking && aimUp;
        casting = isMage && ability;
        if(isMage){if(cycleItem!=0)SelectItem(cycleItem);if(useItem)ToggleItem();TickItem(dt);}
        if (move != 0) facing = Mathf.Sign(move);
        if (motor.isGrounded)
        {
            groundedAt = Time.time;
            if (vertical < 0) vertical = -2;
        }
        if (jumpPressed&&!crouching) jumpPressedAt = Time.time;
        // Small grace windows make platform edges and landing jumps less frustrating.
        if (Time.time - jumpPressedAt < .1f && Time.time - groundedAt < .1f)
        {
            vertical = jumpSpeed;
            jumpPressedAt = groundedAt = -10;
        }
        vertical -= gravity * dt;
        float pace = crouching ? 1.8f : blocking ? 2f : walk && motor.isGrounded ? Mathf.Min(2.3f, speed) : speed;
        float previousX = transform.position.x;
        var collisions=motor.Move(new Vector3(move * pace, vertical, 0) * dt);
        PresentationSpeed = Mathf.Abs(transform.position.x - previousX) / Mathf.Max(dt, .0001f);
        PresentationAirborne = !motor.isGrounded;
        if((collisions&CollisionFlags.Above)!=0&&vertical>0)vertical=0;
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        if (attack && Time.time >= nextAttack)
            Attack();
        UpdatePresentation();
        if (transform.position.y < -7) { health=0; blocking=casting=false; game.OnDown(this); }
    }

    void UpdatePresentation()
    {
        UpdateProtection();
        if(postureVisual){postureVisual.localScale=new Vector3(1,crouching?.58f:1,1);postureVisual.localPosition=new Vector3(0,crouching?-.42f:0,0);}
        float attackAge = Time.time - lastAttack;
        if(fist){fist.gameObject.SetActive(!warriorVisual&&!hasEquipment);fist.localPosition=new Vector3(facing*(attackAge<.18f?1.1f:.55f),-.1f,-.5f);}
        if (sword)
        {
            sword.gameObject.SetActive(!warriorVisual&&hasEquipment);
            sword.localPosition = new Vector3(facing * .65f, 0, -.15f);
            sword.localRotation = Quaternion.Euler(0, 0, attackAge < .2f
                ? facing * Mathf.Lerp(-100, 75, attackAge / .2f) : -facing * 15);
        }
        if (shield)
        {
            shield.gameObject.SetActive(hasEquipment&&(!warriorVisual||shieldUp));
            shield.localPosition = shieldUp ? new Vector3(0,1.12f,-.45f) : new Vector3(facing * .65f, 0, -.45f);
            shield.localRotation = Quaternion.Euler(0,0,shieldUp?90:0);
            shield.localScale = blocking ? new Vector3(.9f, 1.3f, .2f) : new Vector3(.6f, .8f, .2f);
        }
        if (arc)
        {
            arc.gameObject.SetActive(hasEquipment && attackAge < .18f);
            arc.localPosition = new Vector3(facing * .8f, 0, -.8f);
            arc.localRotation = Quaternion.Euler(0, facing < 0 ? 180 : 0, Mathf.Lerp(-70, 70, attackAge / .18f));
        }
        if (armorVisual) armorVisual.SetActive(!warriorVisual&&armor > 0);
        bool flashing = Time.time < immuneUntil && Mathf.FloorToInt(Time.time * 12) % 2 == 0;
        tint.SetColor("_BaseColor", flashing ? new Color(1, .55f, .5f) : !isMage&&!hasEquipment?new Color(.72f,.56f,.38f):bodyColor);
        body.SetPropertyBlock(tint);
    }

    void Attack()
    {
        AttackSerial++;
        nextAttack = Time.time + (isMage ? .32f : hasEquipment ? .52f : .28f);
        combo = Time.time - lastAttack > .7f ? 0 : (combo + 1) % 3;
        lastAttack = Time.time;
        if (isMage)
        {
            FireBolt("Ninguno");
            return;
        }
        foreach(var chest in FindObjectsByType<VillageChest>())
        {
            Vector3 delta=chest.transform.position-transform.position;
            if(Mathf.Abs(delta.y)<2 && delta.x*facing>=-.3f && Mathf.Abs(delta.x)<(hasEquipment?2.5f:1.8f))chest.Hit();
        }
        foreach (var enemy in FindObjectsByType<VillageEnemy>())
        {
            Vector3 offset = enemy.transform.position - transform.position;
            if (Mathf.Abs(offset.y) >= 2 || offset.x * facing < -.3f || Mathf.Abs(offset.x) >= (hasEquipment?2.5f:1.8f)) continue;
            bool wall = false;
            foreach (var hit in Physics.RaycastAll(transform.position, offset.normalized, offset.magnitude))
            {
                if (hit.collider.isTrigger || hit.collider.GetComponentInParent<VillageHero>()
                    || hit.collider.GetComponentInParent<VillageEnemy>()) continue;
                wall = true;
                break;
            }
            if (!wall) enemy.Hit(hasEquipment?(combo == 2 ? 35 : 20):8, false);
        }
    }

    void FireBolt(string kind)
    {
        var projectile=GameObject.CreatePrimitive(PrimitiveType.Sphere);projectile.name="Proyectil del mago · "+kind;
        projectile.transform.position=transform.position+new Vector3(facing*.9f,crouching?-.45f:.3f,0);projectile.transform.localScale=Vector3.one*.35f;Destroy(projectile.GetComponent<Collider>());
        projectile.GetComponent<Renderer>().material.color=kind=="Hielo"?Color.cyan:kind=="Fuego"?new Color(1,.4f,0):Color.magenta;
        var bolt=projectile.AddComponent<VillageBolt>();bolt.direction=facing;bolt.damage=kind=="Fuego"?35:20;bolt.ice=kind=="Hielo";
        if(kind=="Hielo"||kind=="Fuego")VillageSpellVisual.Attach(projectile,bolt.ice,facing);
    }

    public void Revive(Vector3 position, int restoredHealth)
    {
        Teleport(position);health=restoredHealth;immuneUntil=Time.time+RecoveryDuration;blocking=casting=false;
    }
    public float ItemCharge(int index)=>index==0?fireCharge:index==1?iceCharge:protectionCharge;
    void SetCharge(int index,float value){if(index==0)fireCharge=value;else if(index==1)iceCharge=value;else protectionCharge=value;}
    public bool Enchant(string type)
    {
        int index=System.Array.IndexOf(ItemNames,type);if(index<0||ItemCharge(index)>=ItemMaximum(index))return false;
        bool empty=fireCharge+iceCharge+protectionCharge<=0;SetCharge(index,ItemMaximum(index));
        if(empty)selectedItem=index;
        return true;
    }
    public void SelectItem(int direction){if(IsDown)return;selectedItem=(selectedItem+(direction>0?1:2))%3;itemActive=false;enchantment="Ninguno";}
    public void ToggleItem()
    {
        if(IsDown||!isMage)return;
        if(selectedItem==2){itemActive=!itemActive&&protectionCharge>0;enchantment=itemActive?"Protección":"Ninguno";return;}
        if(ItemCharge(selectedItem)<1||Time.time<nextAttack)return;
        SetCharge(selectedItem,ItemCharge(selectedItem)-1);itemActive=false;enchantment="Ninguno";
        nextAttack=Time.time+.32f;lastAttack=Time.time;AttackSerial++;FireBolt(ItemNames[selectedItem]);
    }
    void TickItem(float dt){if(!itemActive||selectedItem!=2)return;protectionCharge=Mathf.Max(0,protectionCharge-dt);if(protectionCharge<=0){itemActive=false;enchantment="Ninguno";}}
    void SetPosture(bool wanted)
    {
        if(crouching&&!wanted)
        {
            // Do not stand up through an overhead platform.
            foreach(var hit in Physics.OverlapCapsule(transform.position+Vector3.up*.05f,transform.position+Vector3.up*.57f,.4f,~0,QueryTriggerInteraction.Ignore))
                if(hit!=motor&&!hit.GetComponentInParent<VillageHero>()){wanted=true;break;}
        }
        crouching=wanted;motor.height=crouching?1.16f:2;motor.center=new Vector3(0,crouching?-.42f:0,0);
    }

    public void Damage(int amount, bool blockable = false, float sourceX = 0, float sourceY = float.NaN)
    {
        if (!VillageOnline.CanSimulate) return;
        if(IsDown)return;
        if(isMage&&itemActive&&selectedItem==2&&protectionCharge>0&&enchantment=="Protección")
        {if(game)game.notice="Protección: impacto bloqueado";return;}
        if(Time.time<immuneUntil)return;
        float dx=sourceX-transform.position.x;
        float dy=float.IsNaN(sourceY)?0:sourceY-(transform.position.y+(crouching?-.42f:0));
        bool above=dy>Mathf.Max(.65f,Mathf.Abs(dx)*.8f);
        if (blockable && blocking && hasEquipment && (shieldUp ? above : !above&&dx*facing>=0))
        {
            armor=Mathf.Max(0,armor-Mathf.Max(1,Mathf.CeilToInt(amount*.5f)));
            if(armor==0){hasEquipment=false;blocking=shieldUp=false;warriorVisual?.BeginArmorBreak();game.notice="Equipo roto: perdiste espada y escudo. Busca armadura o un cofre de equipo";}
            else game.notice="Bloqueo · resistencia del equipo: "+armor+"/60";
            immuneUntil=Time.time+.18f;
            UpdatePresentation();
            return;
        }
        // Protection rejects incoming damage above, before armor or health change.
        // Armor absorbs half the hit while it has enough durability.
        int absorbed = Mathf.Min(armor, amount / 2);
        armor -= absorbed;
        health -= amount - absorbed;
        if(!isMage&&hasEquipment&&armor==0){hasEquipment=false;blocking=shieldUp=false;if(health>0)warriorVisual?.BeginArmorBreak();game.notice="Equipo roto · F: puñetazo";UpdatePresentation();}
        immuneUntil = Time.time + RecoveryDuration;
        if (health <= 0) { health=0; blocking=casting=false; vertical=0; game.OnDown(this); }
        UpdateProtection();
    }

    public void ApplyRemote(VillageHeroState state)
    {
        motor.enabled=false;
        transform.position=Vector3.Lerp(transform.position,state.Position,1-Mathf.Exp(-25*Time.deltaTime));
        if(Vector3.Distance(transform.position,state.Position)>3)transform.position=state.Position;
        health=state.Health;armor=state.Armor;facing=state.Facing;blocking=state.Blocking;casting=state.Casting;
        hasEquipment=state.HasEquipment;stars=state.Stars;extraLives=state.Lives;immuneUntil=Time.time+state.Recovery;
        crouching=state.Crouching;shieldUp=state.ShieldUp;selectedItem=state.SelectedItem;itemActive=state.ItemActive;
        PresentationSpeed=state.MoveSpeed;PresentationAirborne=state.Airborne;
        fireCharge=state.FireCharge;iceCharge=state.IceCharge;protectionCharge=state.ProtectionCharge;
        enchantment=state.Enchantment.ToString();
        if(AttackSerial!=state.Attack){AttackSerial=state.Attack;lastAttack=Time.time;}
        UpdatePresentation();
        if(IsDown){tint.SetColor("_BaseColor",Color.gray);body.SetPropertyBlock(tint);}
    }
}

