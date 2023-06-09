using System.Linq;
using UnityEngine;

namespace com.AlooTheAloo.SimpleTide
{
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

        public bool isRegistered(NetworkObject obj)
        {
            return networkObjectPrefabs.Where(x => x == obj).Count() == 1;
        }

        public bool isRegistered(int prefab_ID)
        {
            return networkObjectPrefabs.Where(x => x.prefabID == prefab_ID).Count() == 1;
        }

        public void OnDisconnected()
        {
            foreach (NetworkObject obj in NetworkObject.NetworkObjects.Values)
            {
                Destroy(obj.gameObject);
            }

            NetworkObject.NetworkObjects.Clear();
            NetworkManager.Singleton.ObservedFields.Clear();
            NetworkManager.Singleton.SyncedFields.Clear();

        }

    }

}