using System;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerButton : MonoBehaviour{
    public Button Button => GetComponent<Button>();

    public event Action<string, ushort> OnClick;
    
    [SerializeField] private TextMeshProUGUI _serverNameTmp;
    private string _serverName;
    [SerializeField] private TextMeshProUGUI _serverAddressTmp;
    private string _serverAddress;
    private ushort _serverPort;

    private void OnEnable(){
        Button.onClick.AddListener(() => {
            Debug.Log("Help");
            OnClick?.Invoke(_serverAddress, _serverPort);
        });
    }

    public void ChangeServer(NetworkData data, IPEndPoint ip){
        _serverName = data.ServerName;
        _serverAddress = ip.Address.ToString();
        _serverPort = (ushort)ip.Port;
        
        Debug.Log("Server Name is: " + _serverName + " Server Address is: " + ip);
        
        _serverNameTmp.text = _serverName;
        _serverAddressTmp.text = ip.ToString();
    }
}