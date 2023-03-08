using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : NetworkBehaviour, IEntity{
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _currentHealth;

    public Team Team => Team.Zombie;

    private NavMeshAgent _agent;

    public override void OnNetworkSpawn(){
        _currentHealth = _maxHealth;
        _agent = GetComponent<NavMeshAgent>();
    }

    public void TakeDamage(int damage){
        _currentHealth -= damage;
        if (_currentHealth <= 0)
            Die();
    }

    [ServerRpc]
    public void SpawnZombieAtServerRpc(Vector3 position){
        transform.position = position;
    }

    private void OnTriggerEnter(Collider other){
        IEntity entity = other.GetComponent<IEntity>();

        if (entity != null && entity.NetworkObject.IsOwner && entity.Team != Team){
            entity.TakeDamage(1);
        }
    }

    public void Die(){
        if (!IsServer) return;
        
        NetworkObject.Despawn();
    }

    private void Update(){
        if (IsServer && FPSPlayer.Players.Count > 0){
            FPSPlayer closestPlayer;
            if (FPSPlayer.Players.Count == 1)
                closestPlayer = FPSPlayer.Players[0];
            else closestPlayer = FPSPlayer.Players.Where(p => p.gameObject.activeInHierarchy).Aggregate((p1, p2) =>
                Vector3.Distance(p1.transform.position, transform.position) <
                Vector3.Distance(p2.transform.position, transform.position)
                    ? p1
                    : p2);
            
            _agent.SetDestination(closestPlayer.transform.position);
        }
    }
}

public enum Team{
    Player,
    Zombie
}

public interface IEntity{
    public Team Team{ get; }
    public NetworkObject NetworkObject{ get; }

    public void TakeDamage(int damage);

    public void Die();
}