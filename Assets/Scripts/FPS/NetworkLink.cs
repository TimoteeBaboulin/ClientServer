using Unity.Netcode;
using UnityEngine;

public class NetworkLink : NetworkBehaviour{
    [SerializeField] private NetworkObject _playerPrefab;
    
    void Start(){
        NetworkManager.OnClientConnectedCallback += delegate(ulong clientID){
            var newPlayer = Instantiate(_playerPrefab);
            newPlayer.SpawnWithOwnership(clientID);
        };
    }
}
