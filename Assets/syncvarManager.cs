using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Windows;


public class syncvarManager : MonoBehaviour
{
    [SyncVar(SyncVarType.Bidirectional)] [SerializeField] int a = 0;

    private void Start()
    {
        LobbyUIManager.singleton.add_action_evt += onAddToA;
    }

    private void onAddToA()
    {
        a++;
    }

    private void OnDestroy()
    {
        LobbyUIManager.singleton.add_action_evt -= onAddToA;
    }

}


