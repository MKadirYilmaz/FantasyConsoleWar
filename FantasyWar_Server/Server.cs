using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class Server
{
    public TcpGameServer? TcpServer;
    public UdpBroadcaster? UdpServer;
    
    public void Start(int tcpPort, int udpTargetPort, ServerPackageManager packageHandler, World world)
    {
        UdpServer = new UdpBroadcaster(udpTargetPort);
        TcpServer = new TcpGameServer();
        TcpServer.Start(tcpPort, packageHandler, world, UdpServer);
    }
}

public class TcpGameServer
{
    private TcpListener? _listener;
    private ServerPackageManager? _serverPackageManager;
    private ConcurrentDictionary<int, TcpConnection> _connections = new();
    private UdpBroadcaster? _udpBroadcaster;

    private World? _world;
    public bool IsGameRunning { get; set; } = false;

    public void Start(int port, ServerPackageManager packageHandler, World world, UdpBroadcaster? udpBroadcaster = null)
    {
        _serverPackageManager = packageHandler;
        _world = world;
        _udpBroadcaster = udpBroadcaster;
        
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
                Player? exitedPlayer = _world.GetPlayer(playerId);
                if(exitedPlayer == null) return;
                
                // Notify all other clients about the player leaving
                SpawnOrDestroyPlayerPacket destroyPacket = new SpawnOrDestroyPlayerPacket(exitedPlayer, false);
                BroadcastPacket(destroyPacket);
                
                _world.Entities.TryRemove(playerId, out _);
                
                Console.WriteLine($"Player {playerId} has disconnected and been removed from the game.");
            }
        }
        
    }

    private void SetupNewPlayer(TcpConnection clientConn)
    {
        if (_world == null) return;

        Player spawnedPlayer = EntityManager.CreatePlayer("TestPlayer", _world.GetRandomEmptyLocation());
        
        if (IsGameRunning)
        {
            spawnedPlayer.IsWaiting = true;
            spawnedPlayer.IsSolid = false;
            spawnedPlayer.Visual = "👻";
        }
        
        _connections.TryAdd(spawnedPlayer.Id, clientConn);
        
        // Handle UDP Port Registration
        clientConn.OnPacketReceived += (packet) =>
        {
            if (packet is ClientUdpPortPacket udpPacket)
            {
                if (clientConn.RemoteAddress != null && _udpBroadcaster != null)
                {
                    IPEndPoint endPoint = new IPEndPoint(clientConn.RemoteAddress, udpPacket.Port);
                    _udpBroadcaster.AddClient(endPoint);
                    Console.WriteLine($"Registered UDP endpoint for player {udpPacket.PlayerId}: {endPoint}");
                }
            }
        };

        Console.WriteLine($"New player {spawnedPlayer.Id} has joined the game. (Waiting: {spawnedPlayer.IsWaiting})");
        
        LoginPacket loginPacket = new LoginPacket(spawnedPlayer.Name, spawnedPlayer.Id, spawnedPlayer.GetActorLocation());
        SendPacketTo(loginPacket, clientConn);
        
        var playersDict = new ConcurrentDictionary<int, Player>(
            _world.Entities.Values.OfType<Player>().ToDictionary(p => p.Id, p => p)
        );
        var projDict = new ConcurrentDictionary<int, Projectile>(
            _world.Entities.Values.OfType<Projectile>().ToDictionary(p => p.Id, p => p)
        );
                
        var entitiesDict = new ConcurrentDictionary<int, Entity>(
            _world.Entities.Values.Where(e => !(e is Player) && !(e is Projectile)).ToDictionary(e => e.Id, e => e)
        );
        
        // Send the current world state to the new player
        WorldPacket worldPacket = new WorldPacket(entitiesDict, playersDict, projDict);
        SendPacketTo(worldPacket, clientConn);
        
        if (spawnedPlayer.IsWaiting)
        {
            ChatPacket waitMsg = new ChatPacket("Game in progress. You are waiting for the next round...", -1);
            SendPacketTo(waitMsg, clientConn);
        }
        else
        {
            // Notify all other clients about the new player
            SpawnOrDestroyPlayerPacket spawnPacket = new SpawnOrDestroyPlayerPacket(spawnedPlayer, true);
            BroadcastPacket(spawnPacket);
        }
    }

    public bool IsConnected(int playerId)
    {
        return _connections.ContainsKey(playerId);
    }

    public List<IPAddress> GetConnectedClientsIPs()
    {
        return _connections.Values
            .Select(c => c.RemoteAddress)
            .Where(ip => ip != null)
            .Cast<IPAddress>()
            .Distinct()
            .ToList();
    }
}


