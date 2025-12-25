using System.Net;
using System.Net.Sockets;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class Server
{
    public TcpGameServer? TcpServer;
    public UdpServer? UdpServer;
    
    public void Start(int tcpPort, int udpTargetPort)
    {
        TcpServer = new TcpGameServer();
        TcpServer.Start(tcpPort);
        UdpServer = new UdpServer(udpTargetPort);
    }
}

public class TcpGameServer
{
    private TcpListener? _listener;
    private List<TcpConnection> _connections = new();

    public void Start(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Console.WriteLine($"Listening on port {port}");
        
        Task.Run(AcceptClientsAsync);
        
    }
    private async Task AcceptClientsAsync()
    {
        if (_listener == null) return;
        while (true)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            TcpConnection connection = new TcpConnection(client);

            connection.OnPacketReceived += ServerPackageManager.HandlePacket;
            
            _connections.Add(connection);
            Console.WriteLine("Connected with a client");

            SetupNewPlayer(connection);
        }
    }
    
    public void SendPacketTo(NetworkPacket packet, TcpConnection connection)
    {
        connection.Send(packet);
    }

    public void BroadcastPacket(NetworkPacket packet)
    {
        foreach (var client in _connections)
        {
            client.Send(packet);
        }
    }

    private void SetupNewPlayer(TcpConnection clientConn)
    {
        Player newPlayer = new Player();
        int id = GameState.Players.Count;
        newPlayer.Id = id;
            
        GameState.Players.Add(newPlayer.Id, newPlayer);
        LoginPacket loginPacket = new LoginPacket("ExampleName", newPlayer.Id);
        SendPacketTo(loginPacket, clientConn);
    }
}

public class UdpServer
{
    public UdpBroadcaster Broadcaster { get; set; }
    
    public UdpServer(int port)
    {
        Broadcaster = new UdpBroadcaster(port);
    }
    
    public void BroadcastPacket(NetworkPacket packet)
    {
        Task.Run(() => Broadcaster.BroadcastStateAsync(packet));
    }
}



