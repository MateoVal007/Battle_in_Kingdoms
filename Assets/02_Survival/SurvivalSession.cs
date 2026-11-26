using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kingdoms.Survival
{
    public sealed class SurvivalSession : MonoBehaviour,INetworkRunnerCallbacks
    {
        public static SurvivalSession Instance {get;private set;}
        public NetworkRunner runner;
        public bool Connected=>runner&&runner.IsRunning;
        public bool IsHost=>Connected&&runner.IsServer;
        public int PlayerCount=>Connected?runner.ActivePlayers.Count():0;
        public string room="plaza-02",status="Prueba local Â· Tab cambia de personaje";
        public bool showMenu;
        bool busy;
        public bool onlineRequested;
        void Awake(){if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Application.runInBackground=true;}
        void Start()
        {
            var role=SurvivalNetworkProbe.Arg("-survival-role");
            if(role!=null){gameObject.AddComponent<SurvivalNetworkProbe>();room=SurvivalNetworkProbe.Arg("-survival-room")??room;Connect(role=="host");}
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
        public async void Connect(bool host)
        {
            if(busy||Connected)return;busy=true;onlineRequested=true;
            try
            {
                var go=new GameObject("Fusion Â· Supervivencia");DontDestroyOnLoad(go);runner=go.AddComponent<NetworkRunner>();runner.ProvideInput=true;runner.AddCallbacks(this);
                var settings=Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();settings.AppVersion="kingdoms-survival-v8-bomber";settings.FixedRegion="us";
                var result=await runner.StartGame(new StartGameArgs{GameMode=host?GameMode.Host:GameMode.Client,SessionName=room.Trim(),PlayerCount=2,EnableClientSessionCreation=false,IsVisible=false,CustomPhotonAppSettings=settings,Scene=SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),SceneManager=go.AddComponent<NetworkSceneManagerDefault>()});
                status=result.Ok?"Sala conectada Â· "+(host?"guerrero":"mago"):"No se pudo conectar: "+result.ShutdownReason;
                if(!result.Ok){onlineRequested=false;Destroy(go);runner=null;}else showMenu=false;
            }
            catch(Exception e){onlineRequested=false;status="ConexiÃ³n: "+e.Message;if(runner)Destroy(runner.gameObject);runner=null;}
            busy=false;
        }
        public void OnInput(NetworkRunner r,NetworkInput input)
        {var arena=SurvivalArena.Instance;input.Set(SurvivalNetworkProbe.Instance?SurvivalNetworkProbe.Instance.Input(IsHost):arena?arena.commands[IsHost?0:1]:default(SurvivalCommand));}
        public void OnPlayerJoined(NetworkRunner r,PlayerRef p){status="Jugadores: "+r.ActivePlayers.Count()+"/2";}
        public void OnPlayerLeft(NetworkRunner r,PlayerRef p){status="CompaÃ±ero desconectado Â· partida en pausa";}
        public void OnInputMissing(NetworkRunner r,PlayerRef p,NetworkInput input){input.Set(default(SurvivalCommand));}
        public void OnShutdown(NetworkRunner r,ShutdownReason reason){status="SesiÃ³n cerrada: "+reason;busy=false;if(SurvivalArena.Instance)SurvivalArena.Instance.externalSimulation=false;}
        public void OnConnectedToServer(NetworkRunner r){}
        public void OnDisconnectedFromServer(NetworkRunner r,NetDisconnectReason reason){status="Desconectado: "+reason;}
        public void OnConnectRequest(NetworkRunner r,NetworkRunnerCallbackArgs.ConnectRequest request,byte[] token){if(r.ActivePlayers.Count()<2)request.Accept();else request.Refuse();}
        public void OnConnectFailed(NetworkRunner r,NetAddress address,NetConnectFailedReason reason){status="No se pudo entrar: "+reason;}
        public void OnSessionListUpdated(NetworkRunner r,List<SessionInfo> list){}
        public void OnCustomAuthenticationResponse(NetworkRunner r,Dictionary<string,object> data){}
        public void OnHostMigration(NetworkRunner r,HostMigrationToken token){status="El anfitriÃ³n saliÃ³ Â· vuelve a crear la sala";}
        public void OnReliableDataReceived(NetworkRunner r,PlayerRef p,ReliableKey key,ReadOnlySpan<byte> data){}
        public void OnReliableDataProgress(NetworkRunner r,PlayerRef p,ReliableKey key,float progress){}
        public void OnSceneLoadStart(NetworkRunner r){}
        public void OnSceneLoadDone(NetworkRunner r){}
        public void OnObjectEnterAOI(NetworkRunner r,NetworkObject obj,PlayerRef p){}
        public void OnObjectExitAOI(NetworkRunner r,NetworkObject obj,PlayerRef p){}
    }
}

