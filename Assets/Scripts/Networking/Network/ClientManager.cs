using Riptide;
using Riptide.Utils;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace com.AlooTheAloo.SimpleTide
{
    public class ClientManager : MonoBehaviour
    {
        public static ClientManager instance;

        private void Awake()
        {
            instance = this;
        }

        [MessageHandler((ushort)MessageTypeToClient.CONNECTION_TO_CLIENT)]
        private static void receivePing(Message message)
        {
            Message reply = Message.Create(MessageSendMode.Reliable, MessageTypeToServer.CONNECTION_TO_SERVER);
            reply.AddString(SteamFriends.GetPersonaName());
            SimpleTide.client.Send(reply);
        }


    }
}
