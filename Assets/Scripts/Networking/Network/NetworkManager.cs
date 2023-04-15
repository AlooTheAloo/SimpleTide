using Riptide;
using Riptide.Transports.Steam;
using Riptide.Utils;
using Steamworks;
using System;
using UnityEngine;


public class NetworkManager : MonoBehaviour
{

    private static NetworkManager _singleton;
    internal static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }

    internal Server Server { get; private set; }
    internal Client Client { get; private set; }

        
    #region Setup
    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized!");
            return;
        }
#if UNITY_EDITOR
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
        RiptideLogger.Initialize(Debug.Log, true);
#endif

        SteamServer steamServer = new SteamServer();
        Server = new Server(steamServer);
        Server.ClientConnected += NewPlayerConnected;
        Server.ClientDisconnected += ServerPlayerLeft;

        Client = new Client(new Riptide.Transports.Steam.SteamClient(steamServer));
        Client.Connected += DidConnect;
        Client.ClientConnected += ClientPlayerJoined;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += ClientPlayerLeft;
        Client.Disconnected += DidDisconnect;

    }

    private void FixedUpdate()
    {
        if (Server.IsRunning)
            Server.Update();

        Client.Update();
    }

    private void OnApplicationQuit()
    {
        StopServer();
        Server.ClientConnected -= NewPlayerConnected;
        Server.ClientDisconnected -= ServerPlayerLeft;

        DisconnectClient();
        Client.Connected -= DidConnect;
        Client.ClientConnected -= ClientPlayerJoined;
        Client.ConnectionFailed -= FailedToConnect;
        Client.ClientDisconnected -= ClientPlayerLeft;
        Client.Disconnected -= DidDisconnect;
        
    }

    #endregion

    #region Server / Client Logic (you can close this)
    // Logic to kill the server
    internal void StopServer()
    {
        SimpleTide.ResetIDs();
        ServerManager.players.Clear();
        Server.Stop();
    }

    // Logic to disconnect client
    internal void DisconnectClient()
    {
        Client.Disconnect();
    }

    #endregion

    #region Server Events
    // Called on server when client connects
    private void NewPlayerConnected(object sender, ServerConnectedEventArgs e)
    {
        ServerManager.Singleton.OnClientConnected(e.Client);
        
    }
    // Called on server when client disconnects
    private void ServerPlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        
    }

    #endregion

    #region Client events


    // Called on client on connection
    private void DidConnect(object sender, EventArgs e)
    {
        string lobbyName = SteamMatchmaking.GetLobbyData(LobbyManager.Singleton.lobbyId, "LOBBY_NAME");
        if (SimpleTide.isServer() && LobbyManager.Singleton.privateLobby)
        {
            LobbyUIManager.singleton.OnHostingStart(lobbyName);
        }
        else LobbyUIManager.singleton.OnConnected(lobbyName);
    }

    // Called on client on new client connection
    public void ClientPlayerJoined(object sender, ClientConnectedEventArgs e)
    {

    }


    // Called on client when can't connect to server
    private void FailedToConnect(object sender, EventArgs e)
    {
        // TODO : Relay to UI there has been a connection problem
        print("FailedToConnect");

    }

    // Called on client when other client leaves
    private void ClientPlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        print("Client player left");
    }


    // Called on client when player disconnects
    private void DidDisconnect(object sender, EventArgs e)
    {
        LobbyUIManager.singleton.OnDisconnected();
        NetworkObjectsManager.singleton.OnDisconnected();
    }

    #endregion

}
