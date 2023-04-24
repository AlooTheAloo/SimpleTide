using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.AlooTheAloo.SimpleTide
{
    public class NetworkObject : MonoBehaviour
    {
        public static Dictionary<int, NetworkObject> NetworkObjects = new Dictionary<int, NetworkObject>();

        public int prefabID { get; private set; }
        public int objectID { get; private set; } = -1;
        public int ownerID { get; private set; }

        public void setObjectID(int objectID)
        {
            if (this.objectID != -1)
            {
                Debug.LogError($"You are trying to set the objectID for the object {gameObject.name} while it already has an ID. " +
                    $" while it already has an ID. This is not possible as it would break communication with clients.");
                return;
            }
            NetworkObjects.Add(objectID, this);
            this.objectID = objectID;
        }
        public void setOwnerID(int ownerID) { this.ownerID = ownerID; }
        public void setprefabID(int prefabID) { this.prefabID = prefabID; }

        private void Awake()
        {
            if (!SimpleTide.hasConnection())
            {
                Debug.LogError($"You are trying to create the networkobject {gameObject.name} while there is no connection. " +
                    $"Connect to a server before creating NetworkObjects.");
                Destroy(gameObject);
            }
        }
    }

}
