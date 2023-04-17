using Riptide;
using System.Linq;
using UnityEngine;

public class SimpleTide : MonoBehaviour
{
    #region Helper variables
    public static Client client
    {
        get
        {
            if (NetworkManager.Singleton == null) return null;
            else return NetworkManager.Singleton.Client;
        }
        private set { }
    }

    public static Server server
    {
        get
        {
            return NetworkManager.Singleton.Server;
        }
        private set { }
    }

    #endregion

    #region Helper methods
    /**
     * Asks a gameobject if it is owned by the client that called this method
     */
    public static bool isMine(NetworkObject no)
    {
        if (no == null || NetworkManager.Singleton == null) { return false; }
        else return no.ownerID == client.Id;
    }

    /**
     * Asks if the current client is also a server
     */
    public static bool isServer()
    {
        return server.IsRunning;
    }


    #endregion

    #region Network create

    private static int next_id = int.MinValue;
    
    public static bool hasConnection()
    {
        return client != null && client.IsConnected;
    }

    public static void ResetIDs()
    {
        next_id = 0;
    }

    /**
     * Spawns a networkObject prefab for every client
     */
    public static void networkCreate(NetworkObject objectPrefab, int owner = -1)
    {
        if (owner == -1)
        {
            owner = client.Id;
        }


        if (!NetworkObjectsManager.singleton.isRegistered(objectPrefab))
        {
            Debug.LogError("You are trying to instantiate a prefab that is not registered." +
                " Add it to the list of objects in the 'Resources/NetworkPrefabs' folder to register it");
            return;
        }
        Message message = Message.Create(MessageSendMode.Reliable, MessageTypeToServer.CREATE_OBJECT);
        message.AddInt(objectPrefab.prefabID);
        message.AddUShort((ushort)owner);
        client.Send(message);
    }


    /**
     * Spawns a networkObject for one client, must be server
     */
    public static void networkCreate(ushort targetClient, NetworkObject netObj, int owner = -1)
    {
        if (!isServer()) { Debug.LogError("You are trying to create an object for a client, but you arent't the server"); return; }

        bool isPrefab = netObj.gameObject.scene.name == null;

        if (owner == -1)
        {
            if (isPrefab)
            {
                owner = client.Id;
            }
            else owner = netObj.ownerID; // Already instantiated, so we can give it the pre-existing ID
        }


        if (!NetworkObjectsManager.singleton.isRegistered(netObj.prefabID))
        {
            Debug.LogError("You are trying to instantiate a prefab that is not registered." +
                " Add it to the list of objects in the 'Resources/NetworkPrefabs' folder to register it");
            return;
        }

        Message createNetworkObjectMessage = Message.Create(MessageSendMode.Reliable, MessageTypeToClient.CREATE_OBJECT);
        createNetworkObjectMessage.AddInt(netObj.prefabID);

        if (isPrefab) // Is a prefab, we give it a new ID
        {
            createNetworkObjectMessage.AddInt(next_id);
            next_id++;
        }
        else createNetworkObjectMessage.AddInt(netObj.objectID); // Not a prefab, we use already existing ID

        createNetworkObjectMessage.AddUShort((ushort)owner);
        server.Send(createNetworkObjectMessage, targetClient);
    }



    [MessageHandler((ushort)MessageTypeToServer.CREATE_OBJECT)]
    private static void createObject_Server(ushort client, Message message)
    {
        Message createNetworkObjectMessage = Message.Create(MessageSendMode.Reliable, MessageTypeToClient.CREATE_OBJECT);
        createNetworkObjectMessage.AddInt(message.GetInt()); // Prefab ID
        createNetworkObjectMessage.AddInt(next_id); // Obj. ID
        createNetworkObjectMessage.AddUShort(message.GetUShort()); // Owner
        server.SendToAll(createNetworkObjectMessage);
        next_id++;
    }


    [MessageHandler((ushort)MessageTypeToClient.CREATE_OBJECT)]
    private static void createObject_Client(Message message)
    {
        int prefab_id = message.GetInt();
        int object_id = message.GetInt();
        ushort owner_id = message.GetUShort();


        NetworkObject obj = Instantiate(NetworkObjectsManager.singleton.getObjectByID(prefab_id));
        obj.setObjectID(object_id);
        obj.setOwnerID(owner_id);
        if (isServer())
        {
            ServerManager.players.Where(x => x.connection_ID == owner_id).ToArray()[0]
                .setnetObj(obj);
        }

        NetworkManager.Singleton.AddFields(obj);
    }
    #endregion

    #region NetworkDestroy
    public static void networkDestroy(NetworkObject go)
    {
        Message destroyObjectMessage = Message.Create(MessageSendMode.Reliable, MessageTypeToServer.DESTROY_OBJECT);
        destroyObjectMessage.AddInt(go.objectID);
        client.Send(destroyObjectMessage);
    }

    [MessageHandler((ushort)MessageTypeToServer.DESTROY_OBJECT)]
    private static void destroyObject_Server(ushort client, Message message)
    {
        int ID = message.GetInt();

        NetworkObject netObj = NetworkObject.NetworkObjects[ID];

        if (netObj.ownerID != client)
        {
            Debug.Log("A client is trying to destroy an object that they aren't an owner of. " +
                "Either there's a problem in your logic or they are cheating.");
            return;
        }

        Message destroyNetworkObjectMessage = Message.Create(MessageSendMode.Reliable, MessageTypeToClient.DESTROY_OBJECT);
        destroyNetworkObjectMessage.AddInt(ID);
        server.SendToAll(destroyNetworkObjectMessage);
    }



    [MessageHandler((ushort)MessageTypeToClient.DESTROY_OBJECT)]
    private static void destroyObject_Client(Message message)
    {
        int ID = message.GetInt();
        NetworkManager.Singleton.RemoveFields(NetworkObject.NetworkObjects[ID]);
        Destroy(NetworkObject.NetworkObjects[ID].gameObject);
    }
    #endregion
}

