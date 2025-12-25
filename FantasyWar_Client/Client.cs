using System.Net.Sockets;
using FantasyWar_Engine;

namespace FantasyWar_Client;

public class Client
{
    public TcpGameClient TcpClient;
    public UdpGameClient UdpClient;
    
    public Client(string serverTcpIp, int serverTcpPort,  int listenUdpPort)
    {
        TcpClient = new TcpGameClient();
        TcpClient.Connect(serverTcpIp, serverTcpPort);
        
        UdpClient = new UdpGameClient();
        UdpClient.Start(listenUdpPort);
    }
}

public class TcpGameClient
{
    private TcpConnection? _connection;

    public void Connect(string ip, int port)
    {
        TcpClient client = new TcpClient();
        client.Connect(ip, port);
        
        Console.WriteLine($"Connected to {client.Client.RemoteEndPoint}");
        
        _connection = new TcpConnection(client);
        _connection.OnPacketReceived += ClientPackageHandler.HandlePacket;
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
    
    public void Start(int port)
    {
        _listener = new UdpListener(port);
        _listener.OnPacketReceived += ClientPackageHandler.HandlePacket;
    }
}