using Steamworks;
using System;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    const string GAME_NAME_VALUE = "TODO"; // Replace with Game Name
    const string GAME_NAME_KEY = "GAME_NAME";


    [SerializeField] string GAME_VERSION = "0.0.0"; // Replace in editor with game version
    const string GAME_VERSION_KEY = "GAME_VERSION";

    const string LOBBY_NAME_KEY = "LOBBY_NAME";

    const string LOBBY_VISIBILITY_KEY = "LOBBY_VISIBILITY";
    public enum LOBBY_VISIBILITY_VALUES
    {
        VISIBLE = 0, // Match still hasn't started
        INVISIBLE = 1, // Match started
        DEAD = 2 // Empty lobby, shouldn't get found
    }

    private static LobbyManager _singleton;
    internal static LobbyManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(LobbyManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEnter;

    private const string HostAddressKey = "HostAddress";
    
    [HideInInspector] public CSteamID lobbyId;
    [HideInInspector] public bool privateLobby = false; // Lobby state


    private void Awake()
    {
        Singleton = this;
        bool m_bInitialized = SteamAPI.Init();
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized!");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
    }

    internal void CreateLobby(bool privateLobby = false)
    {
        this.privateLobby = privateLobby;
        SteamMatchmaking.CreateLobby(
            privateLobby ? ELobbyType.k_ELobbyTypePrivate :
            ELobbyType.k_ELobbyTypeInvisible, NetworkConstants.MAX_PLAYERS);
    }
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            //TODO : Tell UI that creation failed
            return;
        }

        // Add data to the lobby
        lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(lobbyId, HostAddressKey, SteamUser.GetSteamID().ToString()); //
        SteamMatchmaking.SetLobbyData(lobbyId, GAME_NAME_KEY, GAME_NAME_VALUE);
        SteamMatchmaking.SetLobbyData(lobbyId, GAME_VERSION_KEY, GAME_VERSION);
        SteamMatchmaking.SetLobbyData(lobbyId, LOBBY_NAME_KEY, $"{SteamFriends.GetPersonaName()}'s{(privateLobby ? " private " : " ")}lobby");
        SteamMatchmaking.SetLobbyData(lobbyId, LOBBY_VISIBILITY_KEY, ((int) LOBBY_VISIBILITY_VALUES.VISIBLE).ToString());

        SimpleTide.server.Start(0, (ushort) NetworkConstants.MAX_PLAYERS);
        SimpleTide.client.Connect("127.0.0.1"); // Connect to ourselves

    }

    internal void JoinLobby(ulong lobbyId)
    {
        SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t callback)
    {
        if (SimpleTide.server.IsRunning)
            return;

        lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HostAddressKey);

        SimpleTide.client.Connect(hostAddress);
    }

    internal void LeaveLobby()
    {
        // DC client
        NetworkManager.Singleton.DisconnectClient();

        if (SimpleTide.isServer())
        {
            SteamMatchmaking.SetLobbyData(lobbyId, LOBBY_VISIBILITY_KEY, ((int)LOBBY_VISIBILITY_VALUES.DEAD).ToString());
        }
    }
        
    internal void JoinRandomLobby()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(GAME_NAME_KEY, GAME_NAME_VALUE, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListNumericalFilter(LOBBY_VISIBILITY_KEY, (int) LOBBY_VISIBILITY_VALUES.VISIBLE, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(GAME_VERSION_KEY, GAME_VERSION, ELobbyComparison.k_ELobbyComparisonEqual); // Make sure game version is the same

        var APIcall = SteamMatchmaking.RequestLobbyList();
        var m_LobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
        m_LobbyMatchList.Set(APIcall, OnLobbyMatchList);
    }

    internal void OnLobbyMatchList(LobbyMatchList_t list, bool bIOFailure)
    {

        if (bIOFailure)
        {
            // TODO : tell client we couldnt connect
            return;
        }

        uint numLobbies = list.m_nLobbiesMatching;
        
        if (numLobbies <= 0)
        {
            print("No lobbies were found, creating one.");
            CreateLobby();
        }
        else
        {
            for (var i = 0; i < numLobbies; i++)
            {
                CSteamID lobby = SteamMatchmaking.GetLobbyByIndex(i);
                JoinLobby(lobby.m_SteamID);
            }
        }
    }
    
    public static void openInvitationUI()
    {
        SteamFriends.ActivateGameOverlayInviteDialog(Singleton.lobbyId);
    }

    private void Update()
    {
        SteamAPI.RunCallbacks();
    }
        
        
}
