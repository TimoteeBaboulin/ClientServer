using System;
using System.Collections.Generic;
using FPS.Connection.Online;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class UILobbyScreen : MonoBehaviour{
    public TextMeshProUGUI LobbyName;
    public TextMeshProUGUI LobbyCode;
    
    public GameObject PlayerList;
    public List<PlayerCard> PlayerCards;

    public Button LaunchGameButton;

    public PlayerCard PlayerPrefab;
    public int PlayerCount;

    private Lobby _currentLobby;

    private async void OnEnable(){
        
        Debug.Log(OnlineDiscoveryManager.Instance);
        _currentLobby = OnlineDiscoveryManager.Instance.CurrentLobby;
        Debug.Log(_currentLobby.Data);
        
        LobbyName.text = _currentLobby.Name;
        LobbyCode.text = _currentLobby.LobbyCode;

        

        Debug.Log("CurrentLobbyPlayerCount:" + _currentLobby.Players.Count);
        
        PlayerCount = _currentLobby.Players.Count;
        _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
        if (_currentLobby.Players[0] != OnlineDiscoveryManager.Instance.LocalPlayer)
            LaunchGameButton.interactable = false;
        
        foreach (var player in _currentLobby.Players){
            var playerCard = Instantiate(PlayerPrefab, PlayerList.transform);
            playerCard.SetPlayer(player);
            PlayerCards.Add(playerCard);
        }

    }

    // async void UpdateLobby(){
    //     while (true){
    //         System.Threading.Thread.Sleep(2000);
    //         
    //         if (_currentLobby.Players.Count != PlayerCount){
    //             Debug.Log("Player logged or left!");
    //             List<Player> LobbyPlayers = new();
    //             LobbyPlayers.AddRange(_currentLobby.Players);
    //
    //             foreach (var card in PlayerCards){
    //                 if (LobbyPlayers.Contains(card.Player)){
    //                     LobbyPlayers.Remove(card.Player);
    //                     continue;
    //                 }
    //             
    //                 Destroy(card.gameObject);
    //             }
    //         
    //             if (LobbyPlayers.Count == 0) return;
    //
    //             foreach (var newPlayer in LobbyPlayers){
    //                 var playerCard = Instantiate(PlayerPrefab, PlayerList.transform);
    //                 playerCard.SetPlayer(newPlayer);
    //                 PlayerCards.Add(playerCard);
    //             }
    //         }
    //     }
    // }

    private void OnDisable(){
        foreach (Transform playerCard in PlayerList.transform){
            Destroy(playerCard.gameObject);
        }
    }
}