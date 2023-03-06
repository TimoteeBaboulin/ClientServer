using System;
using System.Linq;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Discovery : NetworkDiscovery{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private UnityTransport _transport;

    public string ServerName;
    public ushort ServerPort;
    
    private string _ip;

    public void Host(){
        DontDestroyOnLoad(gameObject);
        _transport.ConnectionData.Address = GetLocalIPv4();
        ChangeScene((op) => {
            _networkManager.StartHost();
            StartDiscovery(true);
        });
    }

    private void OnEnable(){
        StartDiscovery(false);
    }

    public void Connect(){
        if (!IPAddress.TryParse(_ip, out var address)) throw new InvalidAddressException("Please enter an IP adress. For LocalHost enter \"127.0.0.1\"");
        _transport.ConnectionData.Address = _ip;
        ChangeScene((op) => {
            _networkManager.StartClient();
        });
    }

    private string GetLocalIPv4(){
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList.First(
                f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToString();
    }
    
    public void ChangeIP(string ip){
        _ip = ip;
    }

    public void ChangeServerName(string name){
        ServerName = name;
    }

    public void ChangeServerPort(string port){
        if (!ushort.TryParse(port, out var actualPort)) throw new InvalidAddressException("Port must be an int");
        ServerPort = actualPort;
    }

    private void ChangeScene(Action<AsyncOperation> action){
        var operation = SceneManager.LoadSceneAsync("FPSPrototype");

        operation.completed += action;
    }
    
    protected override NetworkData GetServerData(){
        return new NetworkData(){ ServerName = ServerName, Port = ServerPort };
    }

    protected override void ResponseReceived(IPEndPoint sender, NetworkData serverData){
        Debug.Log("Received Response from " + serverData.ServerName + " at " + sender + ":" + serverData.Port);
    }
}

public class InvalidAddressException : Exception{
    public InvalidAddressException(string message) : base(message){}
}