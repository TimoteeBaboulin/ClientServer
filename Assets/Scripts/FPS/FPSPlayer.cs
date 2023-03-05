using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayer : NetworkBehaviour, IEntity{
    public static List<FPSPlayer> Players = new();
    public static event Action<FPSPlayer> OnRespawn; 

    public Team Team => Team.Player;
    
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _pivot;
    [SerializeField] private Transform _cannon;
    [SerializeField] private GameObject _bullet;

    [SerializeField] private float _speed = 20;
    [SerializeField] private float _mouseSpeed;

    [SerializeField] private int _maxHealth;
    [SerializeField] private int _currentHealth;
    
    private CharacterController _controller;

    public override void OnNetworkSpawn(){
        if (!IsOwner){
            _camera.enabled = false;
            _camera.transform.GetComponent<AudioListener>().enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        Players.Add(this);
        base.OnNetworkSpawn();
    }

    private void Awake(){
        _controller = GetComponent<CharacterController>();
        transform.position = Vector3.zero;
    }

    private void OnEnable(){
        RespawnServerRpc();
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

    [ClientRpc]
    public void SpawnAtClientRpc(Vector3 spawn, ClientRpcParams rpcParams = default){
        if (!IsOwner) return;

        transform.position = spawn;
    }
    
    public void TakeDamage(int damage){
        if (!IsServer) return;

        _currentHealth -= damage;
        if (_currentHealth<= 0)
            Die();
    }

    [ClientRpc]
    public void TakeDamageClientRpc(int damage){
        _currentHealth -= damage;
    }

    public void Die(){
        if (!IsServer) return;
        gameObject.SetActive(false);
        Invoke(nameof(Respawn), 2);
    }

    [ClientRpc]
    public void DieClientRpc(){
        gameObject.SetActive(false);
    }

    private void Respawn(){
        gameObject.SetActive(true);
        _currentHealth = _maxHealth;
        SetActiveClientRpc();
        OnRespawn?.Invoke(this);
    }

    [ServerRpc]
    private void RespawnServerRpc(){
        Respawn();
    }
    
    [ClientRpc]
    public void SetActiveClientRpc(){
        gameObject.SetActive(true);
        _currentHealth = _maxHealth;
    }
}