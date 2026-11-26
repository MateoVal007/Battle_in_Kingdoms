using UnityEngine;

public sealed class VillageLift : MonoBehaviour
{
    const float RequestDuration = 10f;
    const float CarryDuration = 7f;
    const float MinimumResponseTime = .25f;
    const float AcceptRange = 4.5f;
    const float TetherRange = 8.5f;

    VillageGame game;
    GameObject bubble;
    Vector2 velocity;
    float liftOff;
    public int Requester { get; private set; }
    public float RequestLeft { get; private set; }
    public float CarryLeft { get; private set; }
    public bool Carrying => CarryLeft > 0;
    public bool ReleasedFromLift { get; private set; }

    public void Initialize(VillageGame owner)
    {
        game = owner;
        bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bubble.name = "Burbuja de traslado";
        bubble.SetActive(false);
        Destroy(bubble.GetComponent<Collider>());
        bubble.transform.SetParent(game.warrior.transform, false);
        bubble.transform.localScale = new Vector3(1.9f, 2.6f, 1.9f);
        var renderer = bubble.GetComponent<Renderer>();
        if (game.mage.protectionMaterial) renderer.sharedMaterial = game.mage.protectionMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var edge = new GameObject("Borde de la burbuja");
        edge.transform.SetParent(bubble.transform, false);
        var rim = edge.AddComponent<LineRenderer>();
        rim.useWorldSpace = false;
        rim.loop = true;
        rim.positionCount = 48;
        rim.widthMultiplier = .018f;
        rim.sharedMaterial = game.mage.GetComponent<Renderer>().sharedMaterial;
        for (int i = 0; i < rim.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / rim.positionCount;
            rim.SetPosition(i, new Vector3(Mathf.Cos(angle) * .5f, Mathf.Sin(angle) * .5f, -.24f));
        }
    }

    public void Cancel()
    {
        Requester = 0;
        RequestLeft = CarryLeft = 0;
        ReleasedFromLift = false;
        velocity = Vector2.zero;
        bubble.SetActive(false);
    }

    public void Tick(float dt, bool warriorPressed, bool magePressed, Vector2 direction)
    {
        if (game.warrior.IsDown || game.mage.IsDown)
        {
            Cancel();
            return;
        }

        if (Carrying)
        {
            CarryLeft = Mathf.Max(0, CarryLeft - dt);
            if (warriorPressed || magePressed || !Carrying)
            {
                CarryLeft = 0;
                ReleasedFromLift = true;
                bubble.SetActive(false);
                game.notice = "Burbuja liberada";
                return;
            }

            // The mage anchors the spell. The passenger accelerates and brakes gradually.
            Vector2 target = Vector2.Scale(Vector2.ClampMagnitude(direction, 1), new Vector2(3.7f, 3.1f));
            liftOff = Mathf.Max(0, liftOff - dt);
            if (liftOff > 0 && target.y >= 0) target.y = Mathf.Max(1.8f, target.y);
            velocity = Vector2.MoveTowards(velocity, target, 5.5f * dt);
            Vector2 offset = game.warrior.transform.position - game.mage.transform.position;
            Vector2 step = velocity * dt;
            if ((offset + step).sqrMagnitude > TetherRange * TetherRange
                && Vector2.Dot(step, offset) > 0)
                step = Vector2.ClampMagnitude(offset + step, TetherRange) - offset;
            var collisions = game.warrior.MoveInLift(step);
            if ((collisions & CollisionFlags.Sides) != 0) velocity.x = 0;
            if ((collisions & (CollisionFlags.Above | CollisionFlags.Below)) != 0) velocity.y = 0;
            return;
        }

        RequestLeft = Mathf.Max(0, RequestLeft - dt);
        if (RequestLeft == 0) Requester = 0;
        // Only a new press by the other player can accept an existing request.
        if (warriorPressed == magePressed) return;
        int sender = warriorPressed ? 1 : 2;
        if (Requester == 0)
        {
            Requester = sender;
            RequestLeft = RequestDuration;
            return;
        }
        if (Requester == sender || RequestLeft > RequestDuration - MinimumResponseTime) return;
        Vector3 separation = game.warrior.transform.position - game.mage.transform.position;
        if (separation.sqrMagnitude > AcceptRange * AcceptRange || !ClearPath(separation))
        {
            game.notice = "Burbuja: acÃ©rquense sin obstÃ¡culos y vuelvan a pulsar G";
            return;
        }

        Requester = 0;
        RequestLeft = 0;
        CarryLeft = CarryDuration;
        ReleasedFromLift = false;
        velocity = Vector2.zero;
        liftOff = .7f;
        bubble.SetActive(true);
        game.notice = "Burbuja activa Â· El mago dirige Â· G para soltar";
    }

    bool ClearPath(Vector3 separation)
    {
        foreach (var hit in Physics.RaycastAll(game.mage.transform.position, separation.normalized,
                     separation.magnitude, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.GetComponentInParent<VillageHero>()) return false;
        return true;
    }

    public void ApplyRemote(int requester, float requestLeft, float carryLeft)
    {
        Requester = requester;
        RequestLeft = requestLeft;
        CarryLeft = carryLeft;
        bubble.SetActive(Carrying);
    }

    public void Draw()
    {
        if (Carrying)
        {
            GUI.Box(new Rect(395, 172, 490, 30),
                $"BURBUJA Â· {Mathf.CeilToInt(CarryLeft)} s Â· Mago: dirigir Â· Cualquiera: G soltar");
            return;
        }
        if (Requester == 0) return;
        var hero = Requester == 1 ? game.warrior : game.mage;
        var point = game.view.WorldToViewportPoint(hero.transform.position + Vector3.up * 1.7f);
        var label = Requester == 1 ? "GUERRERO PIDE BURBUJA" : "MAGO PIDE BURBUJA";
        float x = Mathf.Clamp(point.x * 1280 - 123, 12, 1022);
        float y = Mathf.Clamp((1 - point.y) * 720 - 42, 215, 612);
        GUI.Box(new Rect(x, y, 246, 43), $"{label}\nG para aceptar Â· {Mathf.CeilToInt(RequestLeft)} s");
    }
}

