using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayer : NetworkBehaviour{
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _pivot;
    [SerializeField] private Transform _cannon;
    [SerializeField] private GameObject _bullet;

    [SerializeField] private float _speed = 20;
    [SerializeField] private float _mouseSpeed;
    private CharacterController _controller;
    
    public override void OnNetworkSpawn(){
        Debug.Log("Spawn");
        if (IsServer)
            Debug.Log("Server Monitoring Spawn");
        
        if (!IsOwner){
            _camera.enabled = false;
            _camera.transform.GetComponent<AudioListener>().enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        base.OnNetworkSpawn();
    }

    private void Awake(){
        _controller = GetComponent<CharacterController>();
        transform.position = Vector3.zero;
    }
    
    private void Update(){
        if (!IsOwner || IsServer) return;
        
        Vector3 movement = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
        movement.y = 0;
        _controller.Move(movement.normalized * (Time.deltaTime * _speed));

        if (Input.GetButtonDown("Cancel")){
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        if (Input.GetButtonDown("Fire1")){
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked){
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            
            ShootServerRpc(_cannon.position, _cannon.rotation);
        }
        
        if (Cursor.visible == false){
            transform.Rotate(transform.up, Input.GetAxis("Mouse X") * (_mouseSpeed * Time.deltaTime));
            _pivot.RotateAround(transform.position, transform.right,
                Input.GetAxis("Mouse Y") * (-1 * _mouseSpeed * Time.deltaTime));
        }
    }

    [ClientRpc]
    void ShootClientRpc(){
        Debug.Log("Shoot");
        ShootServerRpc(_cannon.position, _cannon.rotation);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 position, Quaternion rotation){
        Debug.Log("Shoot procedure called");
        if (!IsServer)
            return;
        
        GameObject bullet = Instantiate(_bullet, position, rotation);
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    private void Shoot(){
        Instantiate(_bullet, _cannon.position, _cannon.rotation);
    }
    
    public override void OnNetworkDespawn(){
        if (IsOwner && !IsServer){
            Debug.Log("I despawned");
        }
        base.OnNetworkDespawn();
    }
}