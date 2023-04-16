using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkObjectsManager : MonoBehaviour
{
    private NetworkObject[] networkObjectPrefabs;

    public static NetworkObjectsManager singleton;
    void Awake()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("NetworkPrefabs");
        
        networkObjectPrefabs =
            prefabs.Where(x => x.GetComponent<NetworkObject>() != null)
            .OrderBy(x => x.name) // Best way to sort, will always be deterministic as long as game version is the same
            .Select((x, i) => { 
                x.GetComponent<NetworkObject>().setprefabID(i);
                return x.GetComponent<NetworkObject>();
            })
            .ToArray();
        
        singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    public NetworkObject getObjectByID(int id)
    {
        return networkObjectPrefabs.Where(x => x.prefabID == id).First();
    }

    public bool isRegistered(NetworkObject obj) {
        return networkObjectPrefabs.Where(x => x == obj).Count() == 1;
    }

    public void OnDisconnected()
    {
        foreach(NetworkObject obj in NetworkObject.NetworkObjects.Values)
        {
            Destroy(obj.gameObject);
        }

        NetworkObject.NetworkObjects.Clear();
    }

}
