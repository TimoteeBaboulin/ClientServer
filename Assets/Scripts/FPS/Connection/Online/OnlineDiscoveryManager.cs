﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Player = Unity.Services.Lobbies.Models.Player;
using Toggle = UnityEngine.UI.Toggle;

namespace FPS.Connection.Online{
    public class OnlineDiscoveryManager : MonoBehaviour{
        public static OnlineDiscoveryManager Instance;

        public string LobbyName{ get; private set;}
        public int LobbyMaxPlayer{ get; private set;}
        public CreateLobbyOptions LobbyOptions{get; private set;}
        public Player LocalPlayer{ get; private set;}

        [Header("Lobby Settings")] 
        [SerializeField] private int _maxNameLength;
        [SerializeField] private int _maxLobbySize;
        [SerializeField] private UnityTransport _transport;
        [SerializeField] private UIOnlineLobbyReferences _uiReferences;

        private string _lobbyCode;
        private string _ip;
        private ushort _port;
        private Allocation _allocation;
        private UdpClient _client;
        private UInt16 _nonce = 0;

        public Lobby CurrentLobby => _currentLobby;
        private Lobby _currentLobby;

        //Lobby functions
        public async void CreateLobby(){
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(LobbyName, LobbyMaxPlayer, LobbyOptions);
            // _allocation = await RelayService.Instance.CreateAllocationAsync(LobbyMaxPlayer);
            // _transport.ConnectionData.Address = _allocation.RelayServer.IpV4;
            // _transport.ConnectionData.Port = (ushort)_allocation.RelayServer.Port;
            // _client = new UdpClient();
            //
            // using var writer = new FastBufferWriter();
            //
            // writer.WriteByteSafe((byte[])0xDA72);
            // AuthenticationService.Instance.
            // Byte[]
            // _client.SendAsync()
            //
            // _allocation
            
            Debug.Log(_currentLobby.Id);
            
            _uiReferences.HostScreen.SetActive(false);
            _uiReferences.LobbyScreen.SetActive(true);
        }

        public async void DirectConnect(){
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(_lobbyCode);
            
            _uiReferences.ConnectScreen.SetActive(false);
            _uiReferences.LobbyScreen.SetActive(true);
        }
        
        //UI Screen Changes
        public void InitialiseHostScreen(){
            _uiReferences.LobbyNameInputField.text = LobbyName;
            _uiReferences.LobbySizeInputField.text = LobbyMaxPlayer.ToString();
            _uiReferences.PrivacyToggle.isOn = LobbyOptions.IsPrivate.HasValue ? LobbyOptions.IsPrivate.Value : false;
        }

        //UI Setters
        public void ChangeLobbyName(string newName){
            if (newName.Length > _maxNameLength){
                newName = newName.Substring(0, _maxNameLength);
                _uiReferences.LobbyNameInputField.text = newName;
            } else if (newName.Length <= 0){
                newName = "New Server";
            }

            if (Regex.IsMatch(newName, @"^[a-zA-Z0-9\s]+$"))
                LobbyName = newName;
            else
                Debug.LogException(new InvalidDataException("Can only use alphanumeric or space characters."));
        }
        public void ChangeLobbyCode(string code){
            _lobbyCode = code;
        }
        public void ChangeLobbyMaxPlayers(string playerString){
            playerString = playerString.Trim(new char[]{' ', '\''});
            if (!int.TryParse(playerString, out var maxPlayer)){
                Debug.LogException(new InvalidDataException("Trying to use non-numeric characters in MaxPlayer field"));
                maxPlayer = LobbyMaxPlayer;
                _uiReferences.LobbySizeInputField.text = LobbyMaxPlayer.ToString();
            }

            if (maxPlayer > _maxLobbySize){
                maxPlayer = _maxLobbySize;
                _uiReferences.LobbySizeInputField.text = maxPlayer.ToString();
            } else if (maxPlayer <= 0){
                maxPlayer = 1;
                _uiReferences.LobbySizeInputField.text = "1";
            }

            LobbyMaxPlayer = maxPlayer;
        }
        public void ChangeLobbyVisibility(bool isPrivate){
            LobbyOptions.IsPrivate = isPrivate;
        }
        
        private async void Awake(){
            if (Instance != null) Destroy(gameObject);
            Instance = this;
            
            LobbyName = "My Server";
            LobbyMaxPlayer = 4;
            LobbyOptions = new CreateLobbyOptions{ IsPrivate = false};
            
            Dictionary<string, PlayerDataObject> basePlayerData = new();
            basePlayerData.Add("Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "New Player Name"));
            
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions(){ CreateAccount = true});
            LocalPlayer = new Player(id: AuthenticationService.Instance.PlayerId, data: basePlayerData);
            Debug.Log(LocalPlayer.Data);
            
            LobbyOptions.Player = LocalPlayer;
        }
    }

    [Serializable]
    public struct UIOnlineLobbyReferences{
        public GameObject LobbyScreen;
        public GameObject MainScreen;
        public GameObject HostScreen;
        public GameObject ConnectScreen;

        public TMP_InputField LobbyNameInputField;
        public TMP_InputField LobbySizeInputField;

        public Toggle PrivacyToggle;
    }
}