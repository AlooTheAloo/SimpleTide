using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager singleton;
    [SerializeField] private Transform beforeConnectionPanel;
    [SerializeField] private Transform afterConnectionPanel;
    [SerializeField] private Transform hostingGamePanel;
    [SerializeField] private TextMeshProUGUI lobby_name;
    [SerializeField] private TextMeshProUGUI lobby_name_host;

    private void Awake()
    {
        singleton = GetComponent<LobbyUIManager>();
    }
    public void OnMatchmake()
    {
        LobbyManager.Singleton.JoinRandomLobby();
    }

    public void OnConnected(string lobbyName)
    {
        beforeConnectionPanel.gameObject.SetActive(false);
        afterConnectionPanel.gameObject.SetActive(true);
        hostingGamePanel.gameObject.SetActive(false);
        lobby_name.text = lobbyName;
    }

    public void OnHostingStart(string lobbyName)
    {
        beforeConnectionPanel.gameObject.SetActive(false);
        afterConnectionPanel.gameObject.SetActive(false);
        hostingGamePanel.gameObject.SetActive(true);
        lobby_name_host.text = lobbyName;
    }

    public void OnDisconnect()
    {
        LobbyManager.Singleton.LeaveLobby();
    }

    public void OnDisconnected()
    {
        beforeConnectionPanel.gameObject.SetActive(true);
        afterConnectionPanel.gameObject.SetActive(false);
        hostingGamePanel.gameObject.SetActive(false);
    }

    public void OnCreatePrivateRoom()
    {
        LobbyManager.Singleton.CreateLobby(true);
    }

    public void OnInvite()
    {
        LobbyManager.openInvitationUI();
    }

}
