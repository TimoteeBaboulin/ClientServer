using System;
using System.Linq;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Discovery : NetworkDiscovery<NetworkData>{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private UnityTransport _transport;

    [SerializeField] private ServerButton _serverPrefab;
    [SerializeField] private GameObject _serverList;

    [SerializeField] private TMP_InputField _hostIpField;
    [SerializeField] private TMP_InputField _hostPortField;

    public string ServerName;
    public ushort ServerPort;
    
    private string _ip;

    public void InitializeHost(){
        string ip = GetLocalIPv4();

        _hostIpField.text = ip;
        _transport.ConnectionData.Address = ip;

        _transport.ConnectionData.Port = ServerPort;
        _hostPortField.text = ServerPort.ToString();
    }

    public void RefreshServers(){
        foreach (Transform child in _serverList.transform){
            Destroy(child.gameObject);
        }
    }
    
    public void Host(){
        DontDestroyOnLoad(gameObject);
        Debug.Log(_transport.ConnectionData.Address + ":" + _transport.ConnectionData.Port);
        ChangeScene((op) => {
            _networkManager.StartHost();
            StartDiscovery(true);
        });
    }
    public void Connect(){
        Debug.Log(_transport.ConnectionData.Address + ":" + _transport.ConnectionData.Port);
        ChangeScene((op) => {
            _networkManager.StartClient();
        });
    }
    
    public void ChangeIP(string ip){
        if (!IPAddress.TryParse(ip, out var address))
            throw new InvalidAddressException("Please enter an IP adress. For LocalHost enter \"127.0.0.1\"");
        _transport.ConnectionData.Address = ip;
    }
    public void ChangeServerName(string name){
        ServerName = name;
    }
    public void ChangeServerPort(string port){
        if (!ushort.TryParse(port, out var actualPort)) throw new InvalidAddressException("Port must be an int");
        ServerPort = actualPort;
    }

    private void OnEnable(){
        DontDestroyOnLoad(gameObject);
    }
    private string GetLocalIPv4(){
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList.First(
                f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToString();
    }
    private void ChangeScene(Action<AsyncOperation> action){
        var operation = SceneManager.LoadSceneAsync("FPSPrototype");

        operation.completed += action;
    }
    
    protected override NetworkData GetServerData(){
        return new NetworkData(){ ServerName = ServerName, Port = ServerPort };
    }
    protected override void ResponseReceived(IPEndPoint sender, NetworkData serverData){
        ServerButton button = Instantiate(_serverPrefab, _serverList.transform);
        button.ChangeServer(serverData, sender);
        button.OnClick += delegate(string address, ushort port){
            Debug.Log("Clickity Fuck");
            if (!IPAddress.TryParse(address, out var useless)) return;
            
            _transport.ConnectionData.Address = address;
            _transport.ConnectionData.Port = port;
            try{
                Connect();
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }
            
        };
    }
}

public class InvalidAddressException : Exception{
    public InvalidAddressException(string message) : base(message){}
}