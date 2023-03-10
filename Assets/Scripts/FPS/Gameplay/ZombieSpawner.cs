using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieSpawner : NetworkBehaviour{
    [SerializeField] private Zombie _zombiePrefab;

    [SerializeField] private bool _isActive = true;
    [SerializeField] private int _spawnNumber;
    
    [SerializeField] private Vector2 _delayRange = new(1,2);
    private float _nextSpawnAt = 0;

    private void Update(){
        if (!IsServer || _zombiePrefab == null) return;
        if (Time.time < _nextSpawnAt) return;

        if (!_isActive) return;
        
        Zombie zombie = Instantiate(_zombiePrefab.gameObject, transform.position, Quaternion.identity).GetComponent<Zombie>();
        zombie.NetworkObject.Spawn();
        zombie.SpawnZombieAtServerRpc(transform.position);
        _nextSpawnAt = Time.time + Random.Range(_delayRange.x, _delayRange.y);
    }

    [ContextMenu("Spawn")]
    public void Spawn(){
        for (int x = 0; x < _spawnNumber; x++){
            Zombie zombie = Instantiate(_zombiePrefab.gameObject, transform.position, Quaternion.identity).GetComponent<Zombie>();
            zombie.NetworkObject.Spawn();
            zombie.SpawnZombieAtServerRpc(transform.position);
        }
    }
}