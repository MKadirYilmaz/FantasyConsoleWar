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
    private TcpConnection _connection;

    public void Connect(string ip, int port)
    {
        TcpClient client = new TcpClient();
        client.Connect(ip, port);
        
        Console.WriteLine($"Connected to {client.Client.RemoteEndPoint}");
        
        _connection = new TcpConnection(client);
        _connection.OnPacketReceived += HandlePacket;
    }
    
    private void HandlePacket(NetworkPacket packet)
    {
        Console.WriteLine($"Received: {packet.GetType().Name}");

        switch (packet.PacketType)
        {
            case PacketType.Login:
                LoginPacket loginPacket = (LoginPacket)packet;
                InitializePlayerInformation(loginPacket);
                break;
            default:
                break;
        }
    }
    
    public void SendPacket(NetworkPacket packet)
    {
        _connection.Send(packet);
    }

    private void InitializePlayerInformation(LoginPacket loginPacket)
    {
        Console.WriteLine($"Player Name: {loginPacket.PlayerName}, Player ID: {loginPacket.PlayerId}");
    }
}

public class UdpGameClient
{
    private UdpListener _listener;
    
    public void Start(int port)
    {
        _listener = new UdpListener(port);
        _listener.OnPacketReceived += HandlePacket;
    }
    private void HandlePacket(NetworkPacket? packet)
    {
        if(packet == null) return;
        Console.WriteLine($"Received UDP: {packet.GetType().Name}");
        // Handle incoming UDP packets
    }
}