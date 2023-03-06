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
    private float _nextShotAt;

    public override void OnNetworkSpawn(){
        if (!IsOwner){
            _camera.enabled = false;
            _camera.transform.GetComponent<AudioListener>().enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
            RespawnServerRpc();
        
        
        Players.Add(this);
        base.OnNetworkSpawn();
    }

    private void Awake(){
        if (!IsOwner) Debug.Log("Thief!");
        _controller = GetComponent<CharacterController>();
        transform.position = Vector3.zero;
    }

    private void Update(){
        if (!IsOwner) return;

        Vector3 movement = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
        movement.y = 0;
        _controller.Move(movement.normalized * (Time.deltaTime * _speed));

        if (Input.GetButtonDown("Cancel")){
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        if (Input.GetButton("Fire1")){
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked){
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            
            ShootServerRpc(_cannon.position, _cannon.rotation);
            _nextShotAt = Time.time + 0.2f;
        }
        
        if (Cursor.visible == false){
            transform.Rotate(transform.up, Input.GetAxis("Mouse X") * (_mouseSpeed * Time.deltaTime));
            _pivot.RotateAround(transform.position, transform.right,
                Input.GetAxis("Mouse Y") * (-1 * _mouseSpeed * Time.deltaTime));
        }
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 position, Quaternion rotation){
        GameObject bullet = Instantiate(_bullet, position, rotation);
        bullet.GetComponent<NetworkObject>().Spawn();
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
        if (!IsOwner){
            return;
        }

        TakeDamageServerRpc(damage);
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage){
        _currentHealth -= damage;
        if (_currentHealth <= 0)
            Die();
    }

    public void Die(){
        if (!IsOwner && !IsServer) return;
        if (IsServer) Debug.Log("Die");
        
        DieClientRpc();
        Invoke(nameof(Respawn), 2);
        gameObject.SetActive(false);
    }

    [ClientRpc]
    public void DieClientRpc(){
        gameObject.SetActive(false);
    }

    private void Respawn(){
        Debug.Log("Respawn");
        if (!IsServer) return;
        
        gameObject.SetActive(true);
        _currentHealth = _maxHealth;
        SetActiveClientRpc();
        OnRespawn?.Invoke(this);
    }
    
    [ServerRpc]
    private void RespawnServerRpc(){
        Debug.Log("RespawnServerRpc");
        gameObject.SetActive(true);
        _currentHealth = _maxHealth;
        SetActiveClientRpc();
        OnRespawn?.Invoke(this);
    }
    
    [ClientRpc]
    public void SetActiveClientRpc(){
        gameObject.SetActive(true);
        _currentHealth = _maxHealth;
    }
}