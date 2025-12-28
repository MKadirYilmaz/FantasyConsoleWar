using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class Server
{
    public TcpGameServer? TcpServer;
    public UdpServer? UdpServer;
    
    public void Start(int tcpPort, int udpTargetPort, ServerPackageManager packageHandler, World world)
    {
        TcpServer = new TcpGameServer();
        TcpServer.Start(tcpPort, packageHandler, world);
        UdpServer = new UdpServer(udpTargetPort);
    }
}

public class TcpGameServer
{
    private TcpListener? _listener;
    private ServerPackageManager? _serverPackageManager;
    private ConcurrentDictionary<int, TcpConnection> _connections = new();

    private World? _world;

    public void Start(int port, ServerPackageManager packageHandler, World world)
    {
        _serverPackageManager = packageHandler;
        _world = world;
        
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
            
            if(_serverPackageManager != null)
                connection.OnPacketReceived += _serverPackageManager.OnDataReceived;
            
            connection.OnDisconnect += HandleClientDisconnect;
            //_connections.Add(connection);
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
        foreach (TcpConnection conn in _connections.Values)
        {
            conn.Send(packet);
        }
    }
    
    private void HandleClientDisconnect(TcpConnection connection)
    {
        var item = _connections.FirstOrDefault(kvp => kvp.Value == connection);
        if(item.Key != 0)
        {
            int playerId = item.Key;
            _connections.TryRemove(playerId, out _);

            if (_world != null)
            {
                _world.Players.TryRemove(playerId, out _);
                _world.Entities.TryRemove(playerId, out _);
                
                Console.WriteLine($"Player {playerId} has disconnected and been removed from the game.");
                
                WorldPacket worldPacket = new WorldPacket(_world.Players);
                BroadcastPacket(worldPacket);
            }
        }
        
    }

    private void SetupNewPlayer(TcpConnection clientConn)
    {
        if (_world == null) return;
        
        int newPlayerId = _world.Players.Count + 1;
        while(_world.Players.ContainsKey(newPlayerId)) newPlayerId++;
        
        string playerName = "Player" + newPlayerId;
        Player newPlayer = new Player(newPlayerId, playerName, _world.GetRandomEmptyLocation());
        
        _world.AddOrUpdatePlayer(newPlayerId, newPlayer);
        _world.AddOrUpdateEntity(newPlayerId, newPlayer);
        
        _connections.TryAdd(newPlayerId, clientConn);
        
        Console.WriteLine($"New player {playerName} has joined the game.");
        
        LoginPacket loginPacket = new LoginPacket(newPlayer.Name, newPlayer.Id);
        SendPacketTo(loginPacket, clientConn);
        
        WorldPacket worldPacket = new WorldPacket(_world.Players);
        BroadcastPacket(worldPacket);
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



