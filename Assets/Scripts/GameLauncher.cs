using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    [SerializeField]
    private NetworkRunner networkRunnerPrefab;

    private NetworkRunner networkRunner;

    [SerializeField]
    private NetworkPrefabRef playerAvatarPrefab;

    private async void Start()
    {
        networkRunner = Instantiate(networkRunnerPrefab);
        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SceneManager = networkRunner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("ê¨å˜ÅI");
        }
        else
        {
            Debug.Log("é∏îsÅI");
        }
    }
}