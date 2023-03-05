using System;
using Unity.Netcode;
using UnityEngine;

public class Zombie : NetworkBehaviour, IEntity{
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _currentHealth;
    
    public Team Team => Team.Zombie;

    public void TakeDamage(int damage){
        if (!IsServer) return;

        _currentHealth -= damage;
        if (_currentHealth <= 0)
            Die();
        TakeDamageClientRpc(damage);
    }
    
    [ClientRpc]
    public void TakeDamageClientRpc(int damage){
        _currentHealth -= damage;
    }

    private void OnTriggerEnter(Collider other){
        Debug.Log("Zombie");
        if (!IsServer) return;
        IEntity entity = other.GetComponent<IEntity>();
        if (entity != null && entity.Team != Team)
            entity.TakeDamageClientRpc(1);
    }

    public void Die(){
        if (!IsServer) return;
        
        NetworkObject.Despawn();
    }

    [ClientRpc]
    public void DieClientRpc(){}
}

public enum Team{
    Player,
    Zombie
}

public interface IEntity{
    public Team Team{ get; }

    public void TakeDamage(int damage);
    [ClientRpc]
    public void TakeDamageClientRpc(int damage);

    public void Die();

    [ClientRpc]
    public void DieClientRpc();
}