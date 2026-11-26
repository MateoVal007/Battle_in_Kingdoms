using UnityEngine;
public class VillageZone : MonoBehaviour
{
    public string kind;
    public float checkpointX;
    public bool spawnedByChest;
    bool consumed;
    void Start(){ VillagePropModels.Attach(gameObject,kind); VillageHazardModels.Attach(this); }
    public void ResetPickup(){consumed=false;gameObject.SetActive(true);}
    void OnTriggerEnter(Collider collider) { Apply(collider); }
    void OnTriggerStay(Collider collider) { Apply(collider); }
    void Apply(Collider collider)
    {
        if(!VillageOnline.CanSimulate)return;
        if(consumed||!gameObject.activeInHierarchy)return;
        var hero=collider.GetComponent<VillageHero>();
        if(!hero || !hero.game || hero.IsDown)return;
        if(kind=="Espinos"||kind=="Veneno"){hero.Damage(kind=="Espinos"?20:10);return;}
        if(kind=="Checkpoint")
        {
            if(checkpointX>hero.game.checkpoint){hero.game.checkpoint=checkpointX;hero.game.notice="Checkpoint activado";}
            return;
        }
        if (kind == "Vida extra" || kind == "Vida extra mago")
        {
            if (hero.isMage != (kind == "Vida extra mago")) return;
            hero.extraLives++;
            hero.game.notice = "Reserva descubierta · +1 vida extra para " + (hero.isMage ? "el mago" : "el guerrero");
        }
        else if(kind=="Salud"){consumed=true;if(!hero.game.CollectHealth(hero)){consumed=false;return;}}
        else if(kind=="Equipo"){if(hero.isMage)return;hero.hasEquipment=true;hero.armor=60;hero.game.notice="Equipo obtenido: espada, escudo y armadura";}
        else if(kind=="Armadura"){if(hero.isMage||hero.armor>=60)return;hero.armor=60;hero.hasEquipment=true;}
        else{if(!hero.isMage||!hero.Enchant(kind))return;hero.game.notice="Objeto guardado: "+kind+" · Q/E seleccionar · R activar/guardar";}
        consumed=true;gameObject.SetActive(false);
    }
}

