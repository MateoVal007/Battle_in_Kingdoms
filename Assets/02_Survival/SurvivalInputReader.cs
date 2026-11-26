using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace Kingdoms.Survival
{
    // Callbacks retain clicks and one-shot keys across dynamic/fixed input update phases.
    public sealed class SurvivalInputReader : IDisposable
    {
        readonly SurvivalArena arena;
        readonly InputAction click,jump,use,previous,next,life,change;
        Vector2 guiPointer;float guiPointerAt=-10,lastClickAt=-10;
        LineRenderer cursor;
        readonly HashSet<KeyCode> guiKeys=new HashSet<KeyCode>();
        readonly float[] actionAt={-10,-10,-10,-10,-10,-10};
        bool guiRightHeld;float attackUntil;
        public SurvivalInputReader(SurvivalArena owner)
        {
            arena=owner;click=Button("Destino","<Mouse>/leftButton");jump=Button("Salto","<Keyboard>/space");use=Button("Encantamiento","<Keyboard>/r");previous=Button("Libro anterior","<Keyboard>/q");next=Button("Libro siguiente","<Keyboard>/e");life=Button("Vida propia","<Keyboard>/t");change=Button("Cambiar personaje","<Keyboard>/tab");
            click.performed+=_=>{if(Mouse.current!=null)QueueClick(Mouse.current.position.ReadValue());};
            jump.performed+=_=>Increment(0);previous.performed+=_=>Increment(-1);next.performed+=_=>Increment(1);life.performed+=_=>Increment(2);
            use.performed+=_=>Increment(3);
            change.performed+=_=>Switch();
            foreach(var a in new[]{click,jump,use,previous,next,life,change})a.Enable();
        }
        static InputAction Button(string name,string binding)=>new InputAction(name,InputActionType.Button,binding);
        public static bool CanRead
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorWindow.focusedWindow&&UnityEditor.EditorWindow.focusedWindow.GetType().Name=="GameView";
#else
                return Application.isFocused;
#endif
            }
        }
        int Index=>SurvivalSession.Instance&&SurvivalSession.Instance.Connected?(SurvivalSession.Instance.IsHost?0:1):arena.activeHero;
        void Increment(int action)
        {if(!CanRead||Time.unscaledTime-actionAt[action+1]<.05f)return;actionAt[action+1]=Time.unscaledTime;
            if(arena.phase==ArenaPhase.Defeat)return;
            var cmd=arena.commands[Index];if(action==0)cmd.JumpSerial++;else if(action==-1)cmd.CycleSerial--;else if(action==1)cmd.CycleSerial++;else if(action==2)cmd.LifeSerial++;else cmd.UseSerial++;arena.commands[Index]=cmd;}
        void Switch(){if(!CanRead||Time.unscaledTime-actionAt[5]<.05f)return;actionAt[5]=Time.unscaledTime;arena.SwitchLocalHero();}
        public static bool OverHud(Vector2 point)
        {
            Vector2 ui=new Vector2(point.x*1280/Screen.width,(Screen.height-point.y)*720/Screen.height);
            return ui.y>=610||(ui.y<128&&(ui.x<330||ui.x>950||ui.x>438&&ui.x<842));
        }
        public static bool Resolve(Camera camera,Vector2 pixel,Vector3 actor,out Vector3 target,out Vector3 aim)
        {
            target=aim=actor;if(!camera.pixelRect.Contains(pixel)||OverHud(pixel))return false;
            var ray=camera.ScreenPointToRay(pixel);
            if(new Plane(Vector3.up,new Vector3(0,actor.y,0)).Raycast(ray,out float distance))aim=ray.GetPoint(distance);
            var hits=Physics.RaycastAll(ray,500,~0,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.collider.GetComponentInParent<SurvivalHero>()||hit.collider.GetComponentInParent<SurvivalEnemy>()||hit.normal.y<.45f)continue;
                if(!NavMesh.SamplePosition(hit.point,out var nav,1.6f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-hit.point.y)>.65f)continue;
                target=nav.position;aim=hit.point;return true;
            }
            return false;
        }
        public void QueueClick(Vector2 pixel)
        {
            if(!CanRead||Time.unscaledTime-lastClickAt<.05f)return;var h=arena.heroes[Index];
            if(!Resolve(arena.sharedCamera.GetComponent<Camera>(),pixel,h.transform.position,out var target,out var aim))return;
            lastClickAt=Time.unscaledTime;var cmd=arena.commands[Index];cmd.Destination=target;cmd.Aim=aim;cmd.HasAim=true;cmd.ClickSerial++;arena.commands[Index]=cmd;
        }
        public void GuiEvent(Event e)
        {
            if(e==null||!CanRead)return;
            // IMGUI provides the Game view's scaled pointer/key events when native device state is unavailable.
            if(e.type==EventType.KeyUp)guiKeys.Remove(e.keyCode);
            if(e.type==EventType.KeyDown&&guiKeys.Add(e.keyCode))
            {switch(e.keyCode){case KeyCode.F:attackUntil=Time.unscaledTime+.12f;break;case KeyCode.Space:Increment(0);e.Use();break;case KeyCode.Q:Increment(-1);break;case KeyCode.E:Increment(1);break;case KeyCode.R:Increment(3);break;case KeyCode.T:Increment(2);break;case KeyCode.Tab:Switch();e.Use();break;}}
            if(e.type==EventType.MouseDown&&e.button==1){guiRightHeld=true;attackUntil=Time.unscaledTime+.12f;}
            if(e.type==EventType.MouseUp&&e.button==1)guiRightHeld=false;
            var pixel=new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y);
            if(arena.sharedCamera.GetComponent<Camera>().pixelRect.Contains(pixel)){guiPointer=pixel;guiPointerAt=Time.unscaledTime;}
            if(e.type==EventType.MouseDown&&e.button==0)QueueClick(pixel);
        }
        public SurvivalCommand Read(SurvivalCommand last,Camera camera)
        {
            last.Move=Vector2.zero;last.Attack=false;last.Guard=false;last.HasAim=false;
            var k=Keyboard.current;var mouse=Mouse.current;if(!CanRead){guiKeys.Clear();guiRightHeld=false;attackUntil=0;return last;}
            if(k!=null)
            {
                last.Move=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
                last.Attack=k.fKey.isPressed;last.Guard=k.gKey.isPressed||k.lKey.isPressed;
            }
            last.Move+=new Vector2((guiKeys.Contains(KeyCode.D)||guiKeys.Contains(KeyCode.RightArrow)?1:0)-(guiKeys.Contains(KeyCode.A)||guiKeys.Contains(KeyCode.LeftArrow)?1:0),(guiKeys.Contains(KeyCode.W)||guiKeys.Contains(KeyCode.UpArrow)?1:0)-(guiKeys.Contains(KeyCode.S)||guiKeys.Contains(KeyCode.DownArrow)?1:0));
            last.Move=Vector2.ClampMagnitude(last.Move,1);last.Attack=last.Attack||guiKeys.Contains(KeyCode.F)||guiRightHeld||Time.unscaledTime<attackUntil;last.Guard=last.Guard||guiKeys.Contains(KeyCode.G)||guiKeys.Contains(KeyCode.L);
            if(mouse!=null)
            {
                Vector2 point=Time.unscaledTime-guiPointerAt<.2f?guiPointer:mouse.position.ReadValue();
                bool valid=Resolve(camera,point,arena.heroes[Index].transform.position,out var target,out var aim);
                last.HasAim=camera.pixelRect.Contains(point)&&!OverHud(point);last.Aim=aim;last.Attack=last.Attack||last.HasAim&&mouse.rightButton.isPressed;
                ShowCursor(valid&&last.HasAim,target);
            }
            return last;
        }
        void ShowCursor(bool show,Vector3 at)
        {
            if(!cursor){cursor=new GameObject("Destino y puntería del mouse").AddComponent<LineRenderer>();cursor.positionCount=32;cursor.loop=true;cursor.widthMultiplier=.045f;cursor.sharedMaterial=arena.warning;}
            cursor.enabled=show;if(show)for(int i=0;i<32;i++){float a=i*Mathf.PI/16;cursor.SetPosition(i,at+new Vector3(Mathf.Cos(a)*.35f,.06f,Mathf.Sin(a)*.35f));}
        }
        public void Dispose(){foreach(var a in new[]{click,jump,use,previous,next,life,change})a.Dispose();if(cursor)UnityEngine.Object.Destroy(cursor.gameObject);}
    }
}
