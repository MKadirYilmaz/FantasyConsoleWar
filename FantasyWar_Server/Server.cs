using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class Server
{
    
}

public class TcpGameServer
{
    private TcpListener _listener;
    private List<TcpConnection> _connections = new List<TcpConnection>();

    public void Start(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Console.WriteLine($"Listening on port {port}");
        
        Task.Run(AcceptClientsAsync);
        
    }
    private async Task AcceptClientsAsync()
    {
        while (true)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            TcpConnection connection = new TcpConnection(client);

            connection.OnPacketReceived += packet =>
            {
                Console.WriteLine($"Received: {packet}");
            };
            _connections.Add(connection);
            Console.WriteLine("Connected with a client");
        }
    }

    public void Broadcast(NetworkPacket packet)
    {
        foreach (var client in _connections)
        {
            client.Send(packet);
        }
    }
}

