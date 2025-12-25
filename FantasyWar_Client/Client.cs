using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FantasyWar_Engine;

namespace FantasyWar_Client;

public class Client
{
    
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
        // Handle incoming packets
    }
    
    public void SendPacket(NetworkPacket packet)
    {
        _connection.Send(packet);
    }
}

public class UdpListener
{
    private UdpClient _udpClient;

    public UdpListener(int port)
    {
        _udpClient = new UdpClient(port);
        Task.Run(Listen);
    }

    private async Task Listen()
    {
        while (true)
        {
            UdpReceiveResult result = await _udpClient.ReceiveAsync();
            string stateJson = Encoding.UTF8.GetString(result.Buffer);
            Console.WriteLine($"Received state: {stateJson}");
            // Process game state
        }
    }
}