using Riptide;
using Riptide.Utils;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Player{

    public string uname { get; private set; }
    public int connection_ID { get; private set; }
    
    public NetworkObject netObj { get; private set; }

    public void setnetObj(NetworkObject obj)
    {
        netObj = obj;
    }

    public Player(string uname, int connection_ID)
    {
        this.uname = uname;
        this.connection_ID = connection_ID;
    }
}

public class ServerManager : MonoBehaviour
{
    public static ServerManager Singleton;
    public static List<Player> players = new List<Player>();
    
    [SerializeField] private NetworkObject playerPrefab;

    private void Awake()
    {
        Singleton = this;
    }

    public void OnClientConnected(Connection c)
    {
        // Ping player and ask them their name
        Message pingMessage = Message.Create(MessageSendMode.Reliable, MessageTypeToClient.CONNECTION_TO_CLIENT);
        SimpleTide.server.Send(pingMessage, c);
    }

    [MessageHandler((ushort)MessageTypeToServer.CONNECTION_TO_SERVER)]
    private static void receivePong(ushort fromClientId, Message message)
    {
        // Tell new client about all the objects that exist
        foreach (NetworkObject obj in NetworkObject.NetworkObjects.Values)
        {
            SimpleTide.networkCreate(fromClientId, obj);
            //Message objectCreateMessage = Message.Create(MessageSendMode.Reliable, MessageTypeToClient.CREATE_OBJECT);
            
            //objectCreateMessage.AddInt(Singleton.playerPrefab.prefabID); // Prefab ID
            //objectCreateMessage.AddInt(obj.objectID); // Object ID
            //objectCreateMessage.AddInt(obj.ownerID); // Owner ID
            
            //SimpleTide.server.Send(objectCreateMessage, fromClientId);
        }

        players.Add(new Player(message.GetString(), fromClientId));
        print("New player connected : " + players.Last().uname);

        SimpleTide.networkCreate(Singleton.playerPrefab, fromClientId);

    }
}

