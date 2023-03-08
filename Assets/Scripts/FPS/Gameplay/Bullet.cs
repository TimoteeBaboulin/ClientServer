using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour{
    [SerializeField] private float _speed;
    private float _timeOfDeath;

    public override void OnNetworkSpawn(){
        _timeOfDeath = Time.time + 3;
    }

    private void Update(){
        transform.position += transform.forward * (Time.deltaTime * _speed);
        if (Time.time > _timeOfDeath && IsServer)
            NetworkObject.Despawn();
    }

    private void OnTriggerEnter(Collider other){
        if (!IsServer) return;

        if (other.isTrigger) return;

        IEntity entity = other.GetComponent<IEntity>();
        if (entity is{Team: Team.Zombie})
            entity.TakeDamage(3);
        
        Rigidbody rbody = other.GetComponent<Rigidbody>();
        if (rbody != null){
            rbody.AddForce(transform.forward, ForceMode.Impulse);
        }
        Destroy(gameObject);
    }
}