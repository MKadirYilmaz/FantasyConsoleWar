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
    
    public void Start(int tcpPort, int udpTargetPort, ServerPackageManager packageHandler)
    {
        UdpServer = new UdpBroadcaster(udpTargetPort);
        TcpServer = new TcpGameServer();
        TcpServer.Start(tcpPort, packageHandler, UdpServer);
    }
}

public class TcpGameServer
{
    private TcpListener? _listener;
    private ServerPackageManager? _serverPackageManager;
    private ConcurrentDictionary<int, TcpConnection> _connections = new();
    private UdpBroadcaster? _udpBroadcaster;

    public bool IsGameRunning { get; set; } = false;
    
    public event Action<TcpConnection>? OnClientConnected;
    public event Action<int>? OnClientDisconnected;

    public void Start(int port, ServerPackageManager packageHandler, UdpBroadcaster? udpBroadcaster = null)
    {
        _serverPackageManager = packageHandler;
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
            Console.WriteLine("Connected with a client");

            OnClientConnected?.Invoke(connection);
        }
    }
    
    public void RegisterPlayer(int playerId, TcpConnection connection)
    {
        _connections.TryAdd(playerId, connection);
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
            OnClientDisconnected?.Invoke(playerId);
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


