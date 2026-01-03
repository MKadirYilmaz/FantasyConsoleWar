using System.Net.Sockets;
using FantasyWar_Engine;

namespace FantasyWar_Client;

public class Client
{
    public TcpGameClient TcpClient;
    public UdpGameClient UdpClient;
    
    public Client(string serverTcpIp, int serverTcpPort,  int listenUdpPort, ClientPackageHandler packageHandler)
    {
        TcpClient = new TcpGameClient();
        TcpClient.Connect(serverTcpIp, serverTcpPort, packageHandler);
        
        UdpClient = new UdpGameClient();
        UdpClient.Start(listenUdpPort, packageHandler);
        
        packageHandler.OnPlayerLogin += (playerId) =>
        {
            ClientUdpPortPacket portPacket = new ClientUdpPortPacket(playerId, UdpClient.Port);
            TcpClient.SendPacket(portPacket);
            Console.WriteLine($"Sent UDP Port {UdpClient.Port} to server.");
        };
    }
}

public class TcpGameClient
{
    private TcpConnection? _connection;

    public void Connect(string ip, int port, ClientPackageHandler packageHandler)
    {
        TcpClient client = new TcpClient();
        client.Connect(ip, port);
        
        Console.WriteLine($"Connected to {client.Client.RemoteEndPoint}");
        
        _connection = new TcpConnection(client);
        
        _connection.OnPacketReceived += packageHandler.OnDataReceived;
    }
    
    
    public void SendPacket(NetworkPacket packet)
    {
        if (_connection == null) return;
        _connection.Send(packet);
    }
}

public class UdpGameClient
{
    private UdpListener? _listener;
    public int Port => _listener?.Port ?? 0;
    
    public void Start(int port, ClientPackageHandler packageHandler)
    {
        _listener = new UdpListener(port);
        _listener.OnPacketReceived += packageHandler.OnDataReceived;
    }
}