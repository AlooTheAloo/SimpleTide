using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkConstants : MonoBehaviour
{
    public static int PORT = 7777;
    public static string LOCALHOST = "127.0.0.1";
    public static int MAX_PLAYERS = 2; // Set this to max. players per lobby
}

// Add any new message type here
public enum MessageTypeToServer : ushort
{
    CONNECTION_TO_SERVER, // Contains a string (name of client)
    CREATE_OBJECT // Contains an int (the ID of the object to spawn)
}

public enum MessageTypeToClient : ushort
{
    CONNECTION_TO_CLIENT , // Contains nothing, simple request for name of client
    CREATE_OBJECT, // Contains an int (the ID of the object to spawn)
    NEW_USER // New user connects to room (contains int (conn_id) and string (uname))
}