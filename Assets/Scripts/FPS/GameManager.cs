using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour{
    public static GameManager Instance;

    public Vector3[] PlayerSpawns;


    private void Awake(){
        if (Instance != null) Destroy(this);
        Instance = this;

        if (!IsServer) return;
        
        FPSPlayer.OnRespawn += SpawnPlayer;
    }

    private void OnDisable(){
        FPSPlayer.OnRespawn -= SpawnPlayer;
    }
    
    private void SpawnPlayer (FPSPlayer player)
    {
        Debug.Log("Spawn Player");
        if (!IsServer) return;
        

        ClientRpcParams rpcParams = new ClientRpcParams{
            Send = new ClientRpcSendParams{
                TargetClientIds = new[]{player.NetworkObject.OwnerClientId}
            }
        };

        Vector3 spawn = PlayerSpawns[Random.Range(0, PlayerSpawns.Length)];
            
        player.SpawnAtClientRpc(spawn, rpcParams);
    }
}

