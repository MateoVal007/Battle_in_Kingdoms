using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public struct VillageInput : INetworkInput { public float Move, LiftY; public int LiftPress; public NetworkButtons Buttons; }

public class VillageOnline : MonoBehaviour, INetworkRunnerCallbacks
{
    public const string ProtocolVersion="pueblo-v15-bomber";
    public static VillageOnline Instance { get; private set; }
    public static bool CanSimulate => !Instance || (Instance.Connected && Instance.IsHost && Instance.PlayerCount==2);
    public NetworkRunner runner;
    public bool Connected => runner && runner.IsRunning;
    public bool IsHost => Connected && runner.IsServer;
    public int PlayerCount => Connected ? runner.ActivePlayers.Count() : 0;
    public bool Smoke;
    public string ReportPath;
    string room="pueblo-01", status="Crea una sala o entra con el mismo nombre", region="us";
    bool busy;
    bool jumpPending, attackPending,previousPending,nextPending,usePending,lifePending;
    int liftPress;
    public VillageNetworkWorld world;
    public static string Arg(string name){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);return i>=0&&i+1<a.Length?a[i+1]:null;}
    void Awake()
    {
        if(Instance && Instance!=this){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);Application.runInBackground=true;
        Smoke=Arg("-village-test")!=null;ReportPath=Arg("-village-report");
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("VillageOnlineEditorTest",false))
        {UnityEditor.SessionState.SetBool("VillageOnlineEditorTest",false);Smoke=true;ReportPath="C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/Pueblo_online/editor-host-result.txt";}
#endif
    }
    async void Start(){var role=Arg("-village-role");if(Application.isEditor&&Smoke){role="host";room="pueblo-v3-editor-check";}if(role!=null){room=Arg("-village-room")??room;await Connect(role=="host");}}
    void Update()
    {
        var k=Keyboard.current;if(k==null||!Connected)return;
        bool shielding=IsHost&&k.lKey.isPressed;
        jumpPending |= k.spaceKey.wasPressedThisFrame||(!shielding&&(k.wKey.wasPressedThisFrame||k.upArrowKey.wasPressedThisFrame));
        attackPending |= k.fKey.wasPressedThisFrame||k.kKey.wasPressedThisFrame;
        previousPending|=k.qKey.wasPressedThisFrame;nextPending|=k.eKey.wasPressedThisFrame;usePending|=k.rKey.wasPressedThisFrame;lifePending|=k.tKey.wasPressedThisFrame;
        if (PlayerCount == 2 && Application.isFocused && k.gKey.wasPressedThisFrame) liftPress++;
        if(IsHost&&k.rKey.wasPressedThisFrame&&world)world.Game.RestartPair();
    }
    public async System.Threading.Tasks.Task Connect(bool host)
    {
        if(busy||Connected)return;
        room=room.Trim();if(room.Length<3){status="Escribe un nombre de sala de al menos 3 caracteres";return;}
        busy=true;status=host?"Creando salaâ€¦":"Conectandoâ€¦";
        try
        {
            var go=new GameObject("Photon Fusion Runner");DontDestroyOnLoad(go);
            runner=go.AddComponent<NetworkRunner>();runner.ProvideInput=true;runner.AddCallbacks(this);
            var sceneManager=go.AddComponent<NetworkSceneManagerDefault>();
            var settings=Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();settings.FixedRegion=region;settings.AppVersion=ProtocolVersion;
            var result=await runner.StartGame(new StartGameArgs {GameMode=host?GameMode.Host:GameMode.Client,SessionName=room,PlayerCount=2,
                Scene=SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),SceneManager=sceneManager,
                CustomPhotonAppSettings=settings,EnableClientSessionCreation=false,IsVisible=false});
            status=result.Ok?(host?"Guerrero Â· esperando al mago":"Mago Â· conectado"):"No se pudo conectar: "+result.ShutdownReason+". Revisa sala y versiÃ³n 11.";
            if(!result.Ok){if(runner)Destroy(runner.gameObject);runner=null;}
        }
        catch(Exception e){status="Error de conexiÃ³n: "+e.GetType().Name;Debug.LogException(e);if(runner)Destroy(runner.gameObject);runner=null;}
        busy=false;
    }
    async void Leave(){if(busy)return;busy=true;if(runner)await runner.Shutdown();var scene=SceneManager.GetActiveScene().buildIndex;Instance=null;Destroy(gameObject);SceneManager.LoadScene(scene);}
    void OnDestroy(){if(Instance==this)Instance=null;}
    void OnGUI()
    {
        var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
        if(!Connected)
        {
            GUI.Box(new Rect(390,210,500,280),"PUEBLO Â· MULTIJUGADOR ONLINE");
            GUI.Label(new Rect(420,250,440,30),"Dos jugadores Â· anfitriÃ³n guerrero / invitado mago");
            GUI.Label(new Rect(420,285,110,25),"Nombre de sala");room=GUI.TextField(new Rect(535,285,325,28),room,32);
            GUI.Label(new Rect(420,320,440,25),"US Â· versiÃ³n 11 Â· usa esta compilaciÃ³n en ambos equipos");
            GUI.enabled=!busy;
            if(GUI.Button(new Rect(420,355,210,42),"Crear partida"))_=Connect(true);
            if(GUI.Button(new Rect(650,355,210,42),"Entrar a partida"))_=Connect(false);
            GUI.enabled=true;GUI.Label(new Rect(420,415,440,60),status);
        }
        else
        {
            GUI.Box(new Rect(350,100,580,62),"Online Â· "+room+" Â· "+PlayerCount+"/2 Â· TÃº: "+(IsHost?"Guerrero":"Mago"));
            GUI.Label(new Rect(365,130,550,25),PlayerCount<2?"Esperando al compaÃ±ero; partida en pausa":"A/D Â· Espacio Â· F atacar Â· G habilidad Â· F1 controles");
            if(GUI.Button(new Rect(16,212,120,30),"Salir de sala"))Leave();
        }
        GUI.matrix=old;
    }
    public void OnInput(NetworkRunner r, NetworkInput input)
    {
        var data=new VillageInput();var k=Keyboard.current;
        if(k!=null&&Application.isFocused){data.Move=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
            bool shielding=IsHost&&k.lKey.isPressed;
            data.LiftY=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);
            data.LiftPress = liftPress;
            data.Buttons.Set(0,jumpPending||k.spaceKey.isPressed||(!shielding&&(k.wKey.isPressed||k.upArrowKey.isPressed)));
            data.Buttons.Set(1,attackPending||k.fKey.isPressed||k.kKey.isPressed);data.Buttons.Set(2,shielding);
            data.Buttons.Set(3,k.sKey.isPressed||k.downArrowKey.isPressed);data.Buttons.Set(4,k.wKey.isPressed||k.upArrowKey.isPressed);
            data.Buttons.Set(5,previousPending||k.qKey.isPressed);data.Buttons.Set(6,nextPending||k.eKey.isPressed);data.Buttons.Set(7,usePending||k.rKey.isPressed);data.Buttons.Set(8,lifePending||k.tKey.isPressed);
            data.Buttons.Set(9,k.leftShiftKey.isPressed||k.rightShiftKey.isPressed);}
        jumpPending=attackPending=previousPending=nextPending=usePending=lifePending=false;
        if(Smoke&&world)data=world.TestInput(IsHost);
        input.Set(data);
    }
    public void OnPlayerJoined(NetworkRunner r,PlayerRef p){status="Conectados: "+r.ActivePlayers.Count()+"/2";}
    public void OnPlayerLeft(NetworkRunner r,PlayerRef p){status="El compaÃ±ero saliÃ³; esperando otro jugador";if(IsHost&&world)world.Game.Lift.Cancel();}
    public void OnShutdown(NetworkRunner r,ShutdownReason reason){status="ConexiÃ³n cerrada: "+reason;busy=false;}
    public void OnConnectedToServer(NetworkRunner r){}
    public void OnDisconnectedFromServer(NetworkRunner r,NetDisconnectReason reason){status="Desconectado: "+reason;}
    public void OnConnectRequest(NetworkRunner r,NetworkRunnerCallbackArgs.ConnectRequest request,byte[] token){if(r.ActivePlayers.Count()<2)request.Accept();else request.Refuse();}
    public void OnConnectFailed(NetworkRunner r,NetAddress address,NetConnectFailedReason reason){status="No se pudo entrar: "+reason;}
    public void OnInputMissing(NetworkRunner r,PlayerRef p,NetworkInput input){input.Set(default(VillageInput));}
    public void OnSessionListUpdated(NetworkRunner r,List<SessionInfo> list){}
    public void OnCustomAuthenticationResponse(NetworkRunner r,Dictionary<string,object> data){}
    public void OnHostMigration(NetworkRunner r,HostMigrationToken token){status="El anfitriÃ³n saliÃ³. Vuelve a crear la sala.";}
    public void OnReliableDataReceived(NetworkRunner r,PlayerRef p,ReliableKey key,ReadOnlySpan<byte> data){}
    public void OnReliableDataProgress(NetworkRunner r,PlayerRef p,ReliableKey key,float progress){}
    public void OnSceneLoadStart(NetworkRunner r){}
    public void OnSceneLoadDone(NetworkRunner r){}
    public void OnObjectEnterAOI(NetworkRunner r,NetworkObject obj,PlayerRef p){}
    public void OnObjectExitAOI(NetworkRunner r,NetworkObject obj,PlayerRef p){}
}
