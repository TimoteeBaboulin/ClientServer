using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour{
    [SerializeField] private float _speed;
    private void Update(){
        transform.position += transform.forward * (Time.deltaTime * _speed);
    }

    private void OnTriggerEnter(Collider other){
        Debug.Log("Collision");
        Rigidbody rbody = other.GetComponent<Rigidbody>();
        if (rbody != null){
            Debug.Log("Rigidbody found");
            rbody.AddForce(transform.forward, ForceMode.Impulse);
        }
        Destroy(gameObject);
    }
}