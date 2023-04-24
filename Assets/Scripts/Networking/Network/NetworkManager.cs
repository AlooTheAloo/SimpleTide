using Riptide;
using Riptide.Transports.Steam;
using Riptide.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace com.AlooTheAloo.SimpleTide
{
    public struct FieldInfoMono
    {
        public FieldInfo fieldInfo;
        public MonoBehaviour mono;

        public FieldInfoMono(FieldInfo fieldInfo, MonoBehaviour mono)
        {
            this.fieldInfo = fieldInfo;
            this.mono = mono;
        }
    }

    public class NetworkManager : MonoBehaviour
    {
        #region Variables
        public Dictionary<string, FieldInfoMono> ObservedFields = new Dictionary<string, FieldInfoMono>();
        public Dictionary<string, object> SyncedFields = new Dictionary<string, object>();

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
        #endregion

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


            CreateSyncVars();
        }



        private void FixedUpdate()
        {
            if (Server.IsRunning)
                Server.Update();

            Client.Update();

            foreach (var syncvar in ObservedFields)
            {
                var type = ((SyncVar)syncvar.Value.fieldInfo.GetCustomAttribute(typeof(SyncVar))).type;
                if (type == SyncVarType.Server && !SimpleTide.isServer() ||
                    syncvar.Value.mono.GetComponent<NetworkObject>().ownerID != Client.Id && !SimpleTide.isServer()) continue;

                // Already in there, has a value
                if (SyncedFields.ContainsKey(syncvar.Key))
                {
                    // Variable was modified
                    if (!syncvar.Value.fieldInfo.GetValue(syncvar.Value.mono).Equals(SyncedFields[syncvar.Key]))
                    {
                        object newval = syncvar.Value.fieldInfo.GetValue(syncvar.Value.mono);

                        if (SimpleTide.isServer())
                        {
                            print("Sending data : " + newval);
                            Message syncVarChange = AddObjectToMessage.handle(
                                Message.Create(MessageSendMode.Reliable, MessageTypeToClient.SYNCVAR_SIGNAL),
                                newval);

                            syncVarChange.Add(syncvar.Key);
                            Server.SendToAll(syncVarChange);
                        }
                        else
                        {
                            print("Sending data : " + newval);

                            Message syncVarChange = AddObjectToMessage.handle(
                                Message.Create(MessageSendMode.Reliable, MessageTypeToServer.SYNCVAR_SIGNAL),
                                newval);

                            syncVarChange.Add(syncvar.Key);
                            Client.Send(syncVarChange);
                        }


                        print($"Variable of field {syncvar.Value.fieldInfo.Name} " +
                            $"with hash {syncvar.Key} is now {newval} ");
                        SyncedFields[syncvar.Key] = newval;

                    }
                }
                else // Newly added with null
                {
                    SyncedFields.Add(syncvar.Key, null);
                }
            }
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
                SimpleTide.onHostingStart?.Invoke(lobbyName);
            }
            else SimpleTide.onConnected?.Invoke(lobbyName);
        }

        // Called on client on new client connection
        public void ClientPlayerJoined(object sender, ClientConnectedEventArgs e)
        {

        }


        // Called on client when can't connect to server
        private void FailedToConnect(object sender, EventArgs e)
        {
            SimpleTide.onFailToConnect?.Invoke();       
        }

        // Called on client when other client leaves
        private void ClientPlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            print("Client player left");
        }


        // Called on client a when client a disconnects
        private void DidDisconnect(object sender, EventArgs e)
        {
            // DC steam
            SteamMatchmaking.LeaveLobby(LobbyManager.Singleton.lobbyId);
            SimpleTide.onDisconnected?.Invoke();
            NetworkObjectsManager.singleton.OnDisconnected();

            // Then DC the server
            if (SimpleTide.isServer())
            {
                Singleton.StopServer();
            }
        }

        #endregion

        #region Syncvars

        public void AddFields(NetworkObject no)
        {
            var monos = no.gameObject.GetComponentsInChildren<MonoBehaviour>();

            foreach (var mono in monos)
            {
                if (mono == null)
                {
                    Debug.LogError($"There is a missing script in the object {no.name}. Please remove it to ensure correct syncronising of variables.");
                    continue;
                }
                var syncvars = FindSyncVars(mono);
                foreach (var field in syncvars)
                {
                    ObservedFields.Add(field.Key, field.Value);
                }
            }
        }


        public void RemoveFields(NetworkObject no)
        {
            var monos = no.gameObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (var mono in monos)
            {
                var syncvars = FindSyncVars(mono);
                foreach (var field in syncvars)
                {
                    ObservedFields.Remove(field.Key);
                }
            }
        }

        public Dictionary<string, FieldInfoMono> FindSyncVars(MonoBehaviour mono)
        {
            Dictionary<string, FieldInfoMono> returnVal = new Dictionary<string, FieldInfoMono>();

            Type t = mono.GetType();
            FieldInfo[] objectFields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < objectFields.Length; i++)
            {
                FieldInfo field = objectFields[i];
                SyncVar attribute = Attribute.GetCustomAttribute(field, typeof(SyncVar)) as SyncVar;

                if (attribute != null)
                {
                    if (mono.GetComponent<NetworkObject>() == null)
                    {
                        Debug.LogError($"You are trying to make a syncvar '{field.Name}' " +
                            $"on an object which is not a network object.");
                    }
                    else returnVal.Add(Hash(mono, field), new FieldInfoMono(field, mono));
                }
            }
            return returnVal;
        }

        private string Hash(MonoBehaviour mono, FieldInfo f)
        {
            // Use a hash function to generate a unique ID based on the objectID, type and fieldname
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
            {
                var hash = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(
                    $"{mono.GetComponent<NetworkObject>().objectID}::{mono.GetType().Name}::{f.Name}"
                ));
                return Convert.ToBase64String(hash);
            }
        }

        private void CreateSyncVars()
        {

            MonoBehaviour[] sceneActive = FindObjectsOfType<MonoBehaviour>();
            foreach (MonoBehaviour mono in sceneActive)
            {
                var syncvars = FindSyncVars(mono);
                foreach (var field in syncvars)
                {
                    ObservedFields.Add(field.Key, field.Value);
                }
            }
        }


        [MessageHandler((ushort)MessageTypeToClient.SYNCVAR_SIGNAL)]
        private static void SyncVar_Client(Message message)
        {
            retrieveRes res = AddObjectToMessage.retrieve(message);
            string hash = res.hash;
            Singleton.SyncedFields[hash] = res.value;
            Singleton.ObservedFields[hash].fieldInfo.SetValue(Singleton.ObservedFields[hash].mono, res.value);
        }

        [MessageHandler((ushort)MessageTypeToServer.SYNCVAR_SIGNAL)]
        private static void SyncVar_Server(ushort clientID, Message message)
        {
            Message client_broadcast = Message.Create(MessageSendMode.Reliable, MessageTypeToClient.SYNCVAR_SIGNAL);

            retrieveRes retrieved = AddObjectToMessage.retrieve(message);

            var field = Singleton.ObservedFields[retrieved.hash];
            SimpleTide.onServerSyncVar(clientID, retrieved.value, field.fieldInfo, field.mono);

            Message hashedRetrieved = AddObjectToMessage.handle(client_broadcast, retrieved.value)
                .AddString(retrieved.hash);


            Singleton.Server.SendToAll(hashedRetrieved);
        }
        #endregion
    }


    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVar : Attribute
    {
        public SyncVarType type;
        public SyncVar(SyncVarType type = SyncVarType.Server) { this.type = type; }
    }

    public enum SyncVarType
    {
        Server, // Default, secure. Only server client can send data to server
        //Authoritative, // Secure if handled correctly. Server client can modify, other clients can modify if they have permissions.
        Bidirectional, // Insecure, but very easy :) (if you dont care about security, ex. a coop game, use this)
    }
}

