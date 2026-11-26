using UnityEngine;

public sealed class VillageSecretCover : MonoBehaviour
{
    public VillageChest chest;
    public float left, right, floor;
    Renderer facade;
    VillageGame game;

    void Start()
    {
        facade = GetComponent<Renderer>();
        game = FindAnyObjectByType<VillageGame>();
    }

    void LateUpdate()
    {
        if (game) facade.enabled = !chest.opened && !Inside(game.warrior) && !Inside(game.mage);
    }

    bool Inside(VillageHero hero)
    {
        var position = hero.transform.position;
        return position.x >= left && position.x <= right && position.y >= floor + .65f;
    }
}

