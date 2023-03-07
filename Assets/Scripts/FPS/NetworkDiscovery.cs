using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class NetworkDiscovery : MonoBehaviour{
    [SerializeField] private long _uniqueApplicationId;
    [SerializeField] private ushort _port = 47777;

    public bool IsRunning{ get; private set; }
    public bool IsServer{ get; private set; }
    public bool IsClient{ get; private set; }
    
    private UdpClient _client;
    
    public enum MessageType{
        Broadcast = 0,
        Response = 1
    }

    private void Start(){
        if (_uniqueApplicationId == 0){
            var value1 = (long) Random.Range(int.MinValue, int.MaxValue);
            var value2 = (long) Random.Range(int.MinValue, int.MaxValue);
            _uniqueApplicationId = value1 + (value2 << 32);
        }
    }

    private void OnDisable(){
        StopDiscovery();
    }

    public void StopDiscovery(){
        IsClient = false;
        IsServer = false;
        IsRunning = false;

        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
    }

    public void StartDiscovery(bool isServer){
        StopDiscovery();
        IsServer = isServer;
        IsClient = !isServer;

        var port = isServer ? _port : 0;
        _client = new UdpClient(port){ EnableBroadcast = true, MulticastLoopback = false };

        _ = ListenAsync(isServer ? ReceiveBroadcastAsync : ReceiveResponseAsync);
        IsRunning = true;
    }

    public void ClientBroadcast(){
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, _port);

        using FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
        
        WriteHeader(writer, MessageType.Broadcast);
        var data = writer.ToArray();
        _client.SendAsync(data, data.Length, endPoint);
    }
    
    async Task ListenAsync(Func<Task> onReceiveTask){
        while (true){
            try{
                await onReceiveTask();
            }
            catch (ObjectDisposedException e){
                break;
            }
        }
    }
    
    protected abstract NetworkData GetServerData();
    protected abstract void ResponseReceived(IPEndPoint sender, NetworkData serverData);
    
    async Task ReceiveResponseAsync(){
        UdpReceiveResult receiveResult = await _client.ReceiveAsync();

        var segment = new ArraySegment<byte>(receiveResult.Buffer, 0, receiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Temp);

        //Si ce n'est pas une réponse ou que ca ne vient pas de notre application
        if (!ReadAndCheckHeader(reader, MessageType.Response)) return;

        reader.ReadNetworkSerializable(out NetworkData networkData);
        ResponseReceived(receiveResult.RemoteEndPoint, networkData);
    }
    async Task ReceiveBroadcastAsync(){
        UdpReceiveResult receiveResult = await _client.ReceiveAsync();

        var segment = new ArraySegment<byte>(receiveResult.Buffer, 0, receiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Temp);

        using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
        WriteHeader(writer, MessageType.Response);

        NetworkData response = GetServerData();
        writer.WriteNetworkSerializable(response);
        var data = writer.ToArray();
        try{
            await _client.SendAsync(data, data.Length, receiveResult.RemoteEndPoint);
        }
        catch (Exception e){
            Console.WriteLine(e);
        }
    }

    private void WriteHeader(FastBufferWriter writer, MessageType type){
        writer.WriteValueSafe(_uniqueApplicationId);
        writer.WriteByteSafe((byte) type);
    }

    private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType){
        
        reader.ReadValueSafe(out long receivedId);
        if (receivedId != _uniqueApplicationId) return false;
        
        reader.ReadByteSafe(out byte messageType);
        return messageType == (byte)expectedType;
    }
}

public struct NetworkData : INetworkSerializable{
    public ushort Port;
    public string ServerName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
        serializer.SerializeValue(ref Port);
        serializer.SerializeValue(ref ServerName);
    }
}