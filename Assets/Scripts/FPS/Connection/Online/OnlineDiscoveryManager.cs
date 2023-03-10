using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
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

        private NetworkDriver _driver;
        private NetworkEndPoint _networkEndPoint;

        public Lobby CurrentLobby => _currentLobby;
        private Lobby _currentLobby;

        //Lobby functions
        public async void CreateLobby(){
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(LobbyName, LobbyMaxPlayer, LobbyOptions);
            _allocation = await RelayService.Instance.CreateAllocationAsync(LobbyMaxPlayer);
            Debug.Log(_allocation.RelayServer.IpV4);
            _transport.ConnectionData.Address = _allocation.RelayServer.IpV4;
            _transport.ConnectionData.Port = (ushort)_allocation.RelayServer.Port;
            _driver = NetworkDriver.Create();
            long.TryParse(_allocation.RelayServer.IpV4, out long address);
            
            var endPoint = new IPEndPoint(address, _allocation.RelayServer.Port);
            NetworkEndPoint.TryParse(_allocation.RelayServer.IpV4, (ushort)_allocation.RelayServer.Port, out _networkEndPoint);
            Debug.Log(_driver.Connect(_networkEndPoint));
            Debug.Log(_driver.Listen());
            
            _client = new UdpClient();

            using (var writer = new FastBufferWriter(1024, Allocator.Temp, 1024*64)){
                // //Header
                // writer.TryBeginWrite(72 + _allocation.ConnectionData.Length);
                // Debug.Log(41 + _allocation.ConnectionData.Length);
                // writer.WriteValue((ushort)0xDA72);
                // writer.WriteByte(0);
                // writer.WriteByte(0);
                // Debug.Log(writer.Length);
                // Debug.Log("Header Ending");
                //
                // writer.WriteByte(0);
                // writer.WriteValue(_nonce);
                // _nonce++;
                // writer.WriteByte((byte) _allocation.ConnectionData.Length);
                // Debug.Log("connectionData length: " + _allocation.ConnectionData.Length);
                //
                // writer.WriteBytes(_allocation.ConnectionData);
                // Debug.Log(writer.Length);
                // Debug.Log(writer.Length + _allocation.Key.Length);
                //
                // writer.WriteBytes(_allocation.Key);
                //
                //
                // Debug.Log(writer.Length);
            }
            Task listener = ListenForMessages();
            await listener;

            Debug.Log(_currentLobby.Id);
        }

        public void HostToLobbyScreen(){
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

        private async Task ListenForMessages(){
            bool running = true;
            var timer = new System.Timers.Timer(10000);
            timer.Elapsed += ((sender, args) => { running = false; });
            timer.Start();
            
            while (true){
                
                try{
                    await ReceiveMessage();
                }
                catch (ObjectDisposedException e){
                    Console.WriteLine(e);
                }
                catch (Exception e){
                    Console.WriteLine(e);
                    throw;
                }
                
            }
        }

        private async Task ReceiveMessage(){
            UdpReceiveResult result;
            Debug.Log("Receive Message");
            try{
                result = await _client.ReceiveAsync();
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }
            
            
            var segment = new ArraySegment<byte>(result.Buffer, 0, result.Buffer.Length);
            using var reader = new FastBufferReader(segment, Allocator.Temp);
            
            UInt16 value;
            reader.ReadValueSafe(out value);
            Debug.Log(value);
            if (value != 0)
                HostToLobbyScreen();
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