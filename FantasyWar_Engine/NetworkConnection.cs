using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace FantasyWar_Engine;

public class NetworkConnection
{
    private readonly TcpClient _client;
    private readonly NetworkStream _steam = new NetworkStream(new Socket(SocketType.Stream, ProtocolType.Tcp));

    public async Task SendPacketAsync(NetworkPacket packet) {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(NetworkPacket.ToJson(packet));
        byte[] lengthPrefix = BitConverter.GetBytes(jsonBytes.Length); // 4 byte header

        byte[] finalPacket = new byte[4 + jsonBytes.Length];
        Buffer.BlockCopy(lengthPrefix, 0, finalPacket, 0, 4);
        Buffer.BlockCopy(jsonBytes, 0, finalPacket, 4, jsonBytes.Length);

        await _steam.WriteAsync(finalPacket, 0, finalPacket.Length);
    }

    public async Task ReceiveLoopAsync() {
        byte[] sizeBuffer = new byte[4];
        while (true) {
            // Önce 4 byte oku (Header)
            int read = await _steam.ReadAsync(sizeBuffer, 0, 4);
            if (read == 0) break; // Bağlantı koptu

            int packetSize = BitConverter.ToInt32(sizeBuffer, 0);
            byte[] packetBuffer = new byte[packetSize];

            // Sonra belirtilen uzunluk kadar oku (Body)
            int bytesRead = 0;
            while (bytesRead < packetSize) {
                bytesRead += await _steam.ReadAsync(packetBuffer, bytesRead, packetSize - bytesRead);
            }

            string json = Encoding.UTF8.GetString(packetBuffer);
            
            using JsonDocument doc = JsonDocument.Parse(json);
            PacketType type = (PacketType)doc.RootElement.GetProperty("Type").GetByte();

            switch (type) {
                case PacketType.Movement:
                    var posData = JsonSerializer.Deserialize<MovementPacket>(json);
                    // Handle movement
                    break;
                case PacketType.Chat:
                    var chatData = JsonSerializer.Deserialize<ChatPacket>(json);
                    // Handle chat
                    break;
            }
        }
    }
}

public class TcpConnection
{
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    
    public event Action<NetworkPacket> OnPacketReceived;
    
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
                string line = await _reader.ReadLineAsync();
                if (line == null)
                {
                    Console.WriteLine("Connection closed.");
                    break;
                }

                Console.WriteLine(line);
                NetworkPacket? packet = NetworkPacket.FromJson(line);
                if (packet != null)
                {
                    OnPacketReceived?.Invoke(packet);
                    Console.WriteLine("OnPacketReceived invoked.");
                }
                else
                {
                    Console.WriteLine("Received unknown packet.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            

        }
    }

    public void Send(NetworkPacket packet)
    {
        string json = NetworkPacket.ToJson(packet);
        _writer.WriteLine(json);
        Console.WriteLine("Message sent.");
    }
}

public class UdpBroadcaster
{
    private UdpClient _udpClient = new UdpClient();
    private IPEndPoint _remoteEndPoint;
    
    public UdpBroadcaster(string ip, int port)
    {
        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
    }

    public void SendState(string stateJson)
    {
        byte[] data = Encoding.UTF8.GetBytes(stateJson);
        _udpClient.Send(data, data.Length, _remoteEndPoint);
    }
}

