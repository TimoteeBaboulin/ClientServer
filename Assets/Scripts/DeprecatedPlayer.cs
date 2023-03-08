using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class DeprecatedPlayer : NetworkBehaviour{
    public int PlayerID;
    private PlayerData _playerData;
    
    public override void OnNetworkSpawn(){
        if (IsServer){
            Debug.Log("Server");
        }
        if (IsOwner){
            LocalPlayerManager.LocalPlayer = this;
        }
        base.OnNetworkSpawn();
    }

    public void SendText(string message){
        if (!IsOwner || IsServer) return;
        
        Debug.Log("HaveToSend: " + message);
        ReceiveTextClientRpc(message);
    }

    [ClientRpc]
    void ReceiveTextClientRpc(string message){
        if (!IsOwner) return;
        
        Debug.Log("Received message: " + message);
    }
}

public static class LocalPlayerManager{
    public static DeprecatedPlayer LocalPlayer;
}

public struct PlayerData{
    public string Name;
    public Color NameColor;
}

public struct TextRequest{
    public int SenderID;
    public string Message;
}