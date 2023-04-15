using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    public static List<NetworkObject> NetworkObjects = new List<NetworkObject>();

    public int prefabID { get; private set; }
    public int objectID { get; private set; }
    public int ownerID { get; private set; }

    public void setObjectID(int objectID) { this.objectID = objectID; }
    public void setOwnerID(int ownerID) { this.ownerID = ownerID;}
    public void setprefabID(int prefabID) { this.prefabID = prefabID; }

    private void Awake()
    {
        NetworkObjects.Add(this);
    }
}
