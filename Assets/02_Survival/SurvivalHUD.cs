using UnityEngine;

namespace Kingdoms.Survival
{
    public sealed class SurvivalHUD : MonoBehaviour
    {
        public SurvivalArena arena;
        void Bar(Rect rect,float fraction,Color color)
        {GUI.color=new Color(.08f,.11f,.15f,.94f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,(rect.width-4)*Mathf.Clamp01(fraction),rect.height-4),Texture2D.whiteTexture);GUI.color=Color.white;}
        void HeroPanel(SurvivalHero hero,float x,string title)
        {
            GUI.Box(new Rect(x,14,310,108),"");
            GUI.Label(new Rect(x+12,20,290,24),title+(hero.Down?" · DERRIBADO":" · Salud: "+hero.health+" HP")+(arena.activeHero==hero.id?"  ◄":""));
            Bar(new Rect(x+12,48,284,11),hero.health/100f,hero.IsMage?new Color(.65f,.4f,1):new Color(.2f,.65f,1));
            GUI.Label(new Rect(x+12,63,290,23),"★ "+hero.stars+" /100   ·   Vidas: "+hero.lives+(hero.IsMage?"":"   ·   Armadura: "+hero.armor));
            if(hero.Down)GUI.Label(new Rect(x+12,88,290,23),arena.phase==ArenaPhase.Defeat?"Derrota · reinicio automático":hero.lives>0?"T: revivir aquí · 1 vida · 100 HP":"Sin vidas disponibles");
            else if(hero.IsMage)GUI.Label(new Rect(x+12,88,290,23),"F: normal · R: "+VillageHero.ItemNames[hero.selectedSpell]+" ["+(hero.selectedSpell==0?hero.fire:hero.selectedSpell==1?hero.ice:Mathf.CeilToInt(hero.protection))+"]"+(hero.protectedByMagic?" ACTIVA":""));
            else GUI.Label(new Rect(x+12,88,290,23),hero.equipped?"Espada y escudo · G/L cubrir":"Sin equipo · busca armadura por el mapa");
        }
        void OnGUI()
        {
            if(!arena||arena.heroes.Length!=2)return;
            arena.input?.GuiEvent(Event.current);
            var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.skin.label.fontSize=15;GUI.skin.button.fontSize=14;
            HeroPanel(arena.heroes[0],16,"GUERRERO");HeroPanel(arena.heroes[1],954,"MAGO");
            string phase=arena.phase==ArenaPhase.Preparation?"PREPARACIÓN":arena.phase==ArenaPhase.Rest?"DESCANSO":arena.phase==ArenaPhase.Wave?"HORDA "+arena.wave+" / 5":arena.phase==ArenaPhase.Victory?"VICTORIA":"DERROTA DEL EQUIPO";
            GUI.Box(new Rect(442,16,396,75),phase);
            GUI.Label(new Rect(458,48,365,28),arena.phase==ArenaPhase.Rest||arena.phase==ArenaPhase.Preparation?Mathf.CeilToInt(arena.countdown)+" s · curas +"+arena.HealthRestore+" HP":arena.phase==ArenaPhase.Wave?arena.LivingEnemies+" activos · "+arena.pending+" por aparecer":arena.phase==ArenaPhase.Defeat?"Reiniciando supervivencia en "+Mathf.CeilToInt(arena.countdown)+" s":"Defensa de la plaza");
            var boss=arena.enemies.Find(e=>e&&e.Giant&&e.health>0);
            if(boss){GUI.Label(new Rect(452,102,385,22),"GIGANTE DE LA MAZA");Bar(new Rect(452,126,376,12),boss.health/(float)boss.maxHealth,new Color(.8f,.22f,.13f));}
            GUI.Box(new Rect(210,615,860,34),arena.notice);
            GUI.Box(new Rect(16,661,1000,43),"Clic: destino · WASD: mover · Espacio: salto · F/derecho: atacar · R: magia · Q/E: libro · G/L: escudo · T: revivir · Tab: cambiar");
            var session=SurvivalSession.Instance;
            if(session&&!session.Connected)
            {
                if(GUI.Button(new Rect(1090,661,174,43),session.showMenu?"Cerrar online":"Online · 2 jugadores"))session.showMenu=!session.showMenu;
                if(session.showMenu)
                {
                    GUI.Box(new Rect(410,240,460,210),"SUPERVIVENCIA · PHOTON FUSION");
                    session.room=GUI.TextField(new Rect(435,286,410,32),session.room,32);
                    if(GUI.Button(new Rect(435,335,195,38),"Crear · guerrero"))session.Connect(true);
                    if(GUI.Button(new Rect(650,335,195,38),"Entrar · mago"))session.Connect(false);
                    GUI.Label(new Rect(435,390,410,45),session.status);
                }
            }
            else if(session)GUI.Label(new Rect(900,635,370,25),session.PlayerCount+"/2 · "+session.status);
            GUI.matrix=previous;
        }
    }
}
