using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour{
    [SerializeField] private float _speed;
    private void Update(){
        transform.position += transform.forward * (Time.deltaTime * _speed);
    }

    private void OnTriggerEnter(Collider other){
        if (!IsServer) return;

        IEntity entity = other.GetComponent<IEntity>();
        if (entity is{Team: Team.Zombie})
            entity.TakeDamageClientRpc(1);
        
        Rigidbody rbody = other.GetComponent<Rigidbody>();
        if (rbody != null){
            rbody.AddForce(transform.forward, ForceMode.Impulse);
        }
        Destroy(gameObject);
    }
}