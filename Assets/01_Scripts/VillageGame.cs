using UnityEngine;
using UnityEngine.InputSystem;

public class VillageGame : MonoBehaviour
{
    public VillageHero warrior, mage;
    public Camera view;
    public Transform gate, liftSeal;
    public VillageLift Lift { get; private set; }
    public float checkpoint = 2;
    public bool gateOpen, completed;
    public float runeCharge;
    public int active;
    public bool reviewPanel;
    public string notice = "Busca cofres: ábrelos con espada o disparos";
    readonly string[] names = { "Llegada", "Huertos", "Tejados", "Arroyo", "Plaza", "Molino", "Bosque", "Puerta interior", "Camino real", "Patio de la torre" };
    public bool BothDown => warrior.IsDown && mage.IsDown;
    public VillageHero ActiveHero => active == 0 ? warrior : mage;
    VillageHero OtherHero => active == 0 ? mage : warrior;

    void SwitchLocalHero()
    {
        if (!VillageOnline.Instance && !OtherHero.IsDown) active = 1 - active;
    }

    void Awake()
    {
        Lift = gameObject.AddComponent<VillageLift>();
        Lift.Initialize(this);
    }

    void Start()
    {
        warrior.game = this; mage.game = this;
        Physics.IgnoreCollision(warrior.GetComponent<CharacterController>(), mage.GetComponent<CharacterController>(), true);
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        if(keyboard!=null&&keyboard.f1Key.wasPressedThisFrame)reviewPanel=!reviewPanel;
        if (keyboard != null && !VillageOnline.Instance)
        {
            if (keyboard.tabKey.wasPressedThisFrame) SwitchLocalHero();
            if (BothDown && keyboard.rKey.wasPressedThisFrame) RestartPair();
            bool skill = keyboard.gKey.wasPressedThisFrame;
            var aim = new Vector2(
                (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0),
                (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0));
            Lift.Tick(Time.deltaTime, skill && active == 0, skill && active == 1, aim);
        }
        if(VillageOnline.Instance && VillageOnline.Instance.Connected)active=VillageOnline.Instance.IsHost?0:1;
        var tracked = ActiveHero;
        if (tracked.IsDown && !BothDown) { active = 1 - active; tracked = ActiveHero; }
        float ahead = tracked.IsDown ? 0 : tracked.Facing * 1.7f;
        bool river=tracked.transform.position.x>67&&tracked.transform.position.x<106;
        var target = new Vector3(Mathf.Clamp(tracked.transform.position.x + ahead, 8, 231), river?5.8f:Mathf.Max(5.8f, tracked.transform.position.y + 2.8f), river?-26:-22);
        if (tracked.transform.position.x >= 167 && tracked.transform.position.x < 193)
            target = new Vector3(target.x, Mathf.Max(6.4f, target.y), -26);
        if (Lift.Carrying)
        {
            var center = (warrior.transform.position + mage.transform.position) * .5f;
            target = new Vector3(Mathf.Clamp(center.x, 8, 231), Mathf.Max(5.8f, center.y + 2.4f), -26);
        }
        view.transform.position = Vector3.Lerp(view.transform.position, target, 1 - Mathf.Exp(-5 * Time.deltaTime));
        if(!VillageOnline.CanSimulate)return;
        // Temporalmente desactivado: mantener ambas habilidades durante 3 segundos.
        // bool protect = !warrior.IsDown && warrior.blocking && Mathf.Abs(warrior.transform.position.x-132)<2.2f;
        // bool channel = !mage.IsDown && mage.casting && Mathf.Abs(mage.transform.position.x-137)<2.2f;
        // runeCharge=Mathf.Clamp(runeCharge+(protect&&channel?Time.deltaTime:-Time.deltaTime*.5f),0,3);
        if(!gateOpen)
        {
            bool ready = liftSeal
                ? Lift.ReleasedFromLift && !mage.IsDown && StandingOnPad(warrior, liftSeal.position.x, liftSeal.position.y + .075f)
                : StandingOnPad(warrior, 132, .325f) && StandingOnPad(mage, 137, .325f);
            runeCharge = ready ? 3 : 0;
            if (ready)
            {
                gateOpen = true;
                gate.gameObject.SetActive(false);
                notice = liftSeal ? "Sello activado · paso al Bosque abierto" : "Paso abierto: ambos en sus pisos de color";
            }
        }
        if (!completed && !warrior.IsDown && !mage.IsDown && warrior.transform.position.x > 232 && mage.transform.position.x > 232)
        { completed = true; notice = "PUEBLO LIBERADO · Ambos llegaron a la salida"; }
    }
    static bool StandingOnPad(VillageHero hero, float x, float surface) =>
        !hero.IsDown && hero.Grounded && Mathf.Abs(hero.transform.position.x - x) < 1.3f
        && Mathf.Abs(hero.transform.position.y - 1 - surface) < .2f;
    public void OnDown(VillageHero hero)
    {
        Lift.Cancel();
        notice = BothDown ? "Ambos están derribados. R: reintentar desde el checkpoint" : "Compañero derribado: encuentra salud o abre un cofre para reanimarlo";
        if(hero.extraLives>0)notice+=" · "+(VillageOnline.Instance?"T":hero.isMage?"Y":"T")+": usar vida extra propia";
        if (!BothDown) active = hero.isMage ? 0 : 1;
        if (hero.transform.position.y < -7) hero.Teleport(SafeSpawn(checkpoint));
    }
    public bool CollectHealth(VillageHero collector)
    {
        if (collector.IsDown) return false;
        var partner = collector == warrior ? mage : warrior;
        if (partner.IsDown)
        {
            partner.Revive(SafeSpawn(collector.transform.position.x + (collector.Facing > 0 ? -1.5f : 1.5f), collector.transform.position.y + 1), 50);
            notice = "Compañero reanimado · 50 HP y 3 segundos de protección";
            return true;
        }
        if (collector.health >= 100) return false;
        collector.health = Mathf.Min(100, collector.health + 30);
        notice = "Salud recuperada";
        return true;
    }
    public void RestartPair()
    {
        if (!VillageOnline.CanSimulate || !BothDown) return;
        warrior.Revive(SafeSpawn(checkpoint), 100);
        mage.Revive(SafeSpawn(checkpoint + 2), 100);
        foreach (var chest in FindObjectsByType<VillageChest>(FindObjectsInactive.Include))
            if (!chest.keepOnRetry) chest.ResetChest();
        foreach (var zone in FindObjectsByType<VillageZone>(FindObjectsInactive.Include))
            if (!zone.spawnedByChest) zone.ResetPickup();
        notice = "Equipo recuperado en checkpoint · cofres y objetos repuestos";
    }
    public void JumpSection(int section)
    {
        if(VillageOnline.Instance)return;
        Lift.Cancel();
        float x = section * 24 + 1;
        float fromY = section == 7 ? 4 : 15;
        warrior.Teleport(SafeSpawn(x, fromY)); mage.Teleport(SafeSpawn(x + 2, fromY));
    }
    public Vector3 SafeSpawn(float x, float fromY = 15)
    {
        var hits = Physics.RaycastAll(new Vector3(x, fromY, 0), Vector3.down, 25, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
            if (!hit.collider.GetComponentInParent<VillageHero>() && !hit.collider.GetComponentInParent<VillageEnemy>() && !hit.collider.GetComponentInParent<VillageChest>())
                return hit.point + Vector3.up * 1.25f;
        return new Vector3(checkpoint, 2, 0);
    }
    void PlayerHUD(Rect bounds, VillageHero hero, string title)
    {
        GUI.Box(bounds, "");
        GUI.Label(new Rect(bounds.x + 12, bounds.y + 5, bounds.width - 24, 24), title + (hero.IsDown ? " · DERRIBADO" : " · " + hero.health + " HP"+(hero.RecoveryRemaining>.25f?" · REC "+Mathf.CeilToInt(hero.RecoveryRemaining)+"s":"")));
        GUI.color = hero.IsDown ? Color.gray : hero.isMage ? new Color(.65f,.4f,1) : new Color(.25f,.65f,1);
        GUI.DrawTexture(new Rect(bounds.x + 12, bounds.y + 33, (bounds.width - 24) * Mathf.Clamp01(hero.health / 100f), 7), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(bounds.x + 12, bounds.y + 45, bounds.width - 24, 22), hero.isMage ? "Magia: " + VillageHero.ItemNames[hero.selectedItem] + (hero.itemActive?" · ACTIVA":"") : hero.hasEquipment ? "Equipo: " + hero.armor + "/60 · F espada / L escudo" : "Sin equipo · F puñetazo · busca el cofre");
        var starStyle=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold};starStyle.normal.textColor=new Color(1,.84f,.18f);
        GUI.Label(new Rect(bounds.x+12,bounds.y+68,215,27),"ESTRELLAS "+hero.stars+" /100",starStyle);
        GUI.Label(new Rect(bounds.x+224,bounds.y+71,80,24),"Vidas: "+hero.extraLives);
    }
    void OnGUI()
    {
        var matrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        GUI.skin.label.fontSize = 15; GUI.skin.button.fontSize = 14; GUI.color = Color.white;
        PlayerHUD(new Rect(16,16,310,98), warrior, "GUERRERO");
        PlayerHUD(new Rect(954,16,310,98), mage, "MAGO");
        VillageMageHUD.Draw(mage);
        GUI.Box(new Rect(16,122,310,80),"");
        GUI.Label(new Rect(28,126,300,24),"Mayús caminar · S agacharse · L + W escudo arriba");
        GUI.Label(new Rect(28,150,300,24),"L + F cubrirse y atacar");
        GUI.Label(new Rect(28,174,300,24), "G pedir / aceptar / soltar burbuja");
        Lift.Draw();
        int section = Mathf.Clamp(Mathf.FloorToInt(ActiveHero.transform.position.x / 24),0,names.Length-1);
        GUI.Label(new Rect(510,20,280,28), (section + 1).ToString("00") + " · " + names[section]);
        if (VillageOnline.Instance)
            GUI.Label(new Rect(510,45,350,24), "Online · cámara de tu personaje");
        else
        {
            GUI.Label(new Rect(510,45,350,24), active == 0 ? "Controlas al guerrero" : "Controlas al mago");
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !OtherHero.IsDown;
            if (GUI.Button(new Rect(510,72,230,32), active == 0 ? "Cambiar al mago" : "Cambiar al guerrero"))
                SwitchLocalHero();
            GUI.enabled = enabled;
        }
        if (runeCharge > 0 && !gateOpen)
            GUI.Label(new Rect(510,VillageOnline.Instance ? 72 : 108,300,24), "Habilidad unida: " + Mathf.RoundToInt(runeCharge / 3 * 100) + "%");
        GUI.Box(new Rect(16,670,1248,34), "");
        GUI.Label(new Rect(28,676,1224,24), notice + "   |   F1: controles y revisión");
        if (VillageOnline.CanSimulate && BothDown && GUI.Button(new Rect(460,330,360,48), "Reintentar equipo desde checkpoint")) RestartPair();
        if (reviewPanel)
        {
            GUI.Box(new Rect(16,550,1248,112), "");
            GUI.Label(new Rect(28,558,1224,24), "Guerrero: A/D · Espacio/W salto · S agachar · L cubrir · L+W arriba · L+F atacar cubierto · T vida extra");
            GUI.Label(new Rect(28,584,1224,24), "Mago: flechas (online A/D) · K/F disparo · S/abajo agachar · G burbuja · flechas dirigen · Q/E elegir · R magia especial · T vida extra online / Y local");
            for (int i=0;i<names.Length;i++) if(GUI.Button(new Rect(28+i*123,620,118,28),(i+1).ToString("00")+" "+names[i])) JumpSection(i);
        }
        GUI.matrix=matrix;
    }
}

