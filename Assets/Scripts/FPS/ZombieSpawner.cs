using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieSpawner : NetworkBehaviour{
    [SerializeField] private Zombie _zombiePrefab;
    
    [SerializeField] private Vector2 _delayRange = new(1,2);
    private float _nextSpawnAt = 0;

    private void Update(){
        if (!IsServer || _zombiePrefab == null) return;
        if (Time.time < _nextSpawnAt) return;

        Zombie zombie = Instantiate(_zombiePrefab.gameObject, transform.position, Quaternion.identity).GetComponent<Zombie>();
        zombie.NetworkObject.Spawn();
        zombie.SpawnZombieAtServerRpc(transform.position);
        _nextSpawnAt = Time.time + Random.Range(_delayRange.x, _delayRange.y);
    }
}