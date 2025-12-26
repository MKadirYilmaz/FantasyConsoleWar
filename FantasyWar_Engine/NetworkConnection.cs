using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FantasyWar_Engine;

public class TcpConnection
{
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    
    public event Action<NetworkPacket> OnPacketReceived;
    public event Action<TcpConnection> OnDisconnect;
    
    public TcpConnection(TcpClient client)
    {
        _client = client;
        NetworkStream stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
        Task.Run(Listen);
    }

    private async Task Listen()
    {
        while (true)
        {
            try
            {
                string? line = await _reader.ReadLineAsync();
                if (line == null)
                {
                    Console.WriteLine("Connection closed.");
                    break;
                }
                NetworkPacket? packet = NetworkPacket.FromJson(line);
                if (packet != null)
                {
                    OnPacketReceived?.Invoke(packet);
                }
                else
                {
                    Console.WriteLine("Received unknown packet.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                break;
            }
        }
        Disconnect();
    }

    public void Send(NetworkPacket packet)
    {
        try
        {
            string json = NetworkPacket.ToJson(packet);
            _writer.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Disconnect();
        }
    }
    
    private void Disconnect()
    {
        try
        {
            OnDisconnect?.Invoke(this);
            _client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

public class UdpBroadcaster
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _broadcastEndPoint;
    private bool _disposed = false;
    

    public UdpBroadcaster(int port)
    {
        _udpClient = new UdpClient();
        _udpClient.EnableBroadcast = true;
        _broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
        
        Console.WriteLine($"UDP Broadcaster initialized on port {port}");
    }

    public async Task BroadcastStateAsync(NetworkPacket packet)
    {
        if (_disposed) return;
        try
        {
            string json = NetworkPacket.ToJson(packet);
            byte[] data = Encoding.UTF8.GetBytes(json);
            
            await _udpClient.SendAsync(data, data.Length, _broadcastEndPoint);
            Console.WriteLine($"Broadcasted: {packet.PacketType} ({data.Length} bytes)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Broadcast error: {ex.Message}");
            Dispose();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _udpClient?.Dispose();
    }
}

public class UdpListener
{
    private readonly UdpClient _udpClient;
    private readonly CancellationTokenSource _cts;

    public event Action<NetworkPacket?>? OnPacketReceived;

    public UdpListener(int port)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        
        _udpClient = new UdpClient { Client = socket };
        _udpClient.EnableBroadcast = true;
        _cts = new CancellationTokenSource();
        
        Console.WriteLine($"UDP Listener started on port {port}");
        Task.Run(() => ListenAsync(_cts.Token));
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync(ct);
                string json = Encoding.UTF8.GetString(result.Buffer);

                NetworkPacket? packet = NetworkPacket.FromJson(json);
                OnPacketReceived?.Invoke(packet);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("UDP Listener stopped.");
                Dispose();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP Listen error: {ex.Message}");
                Dispose();
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _udpClient?.Dispose();
        _cts?.Dispose();
    }
}

