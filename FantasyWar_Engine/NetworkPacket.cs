using System.Collections.Concurrent;
using System.Text.Json;

namespace FantasyWar_Engine;

public enum PacketType : byte
{
    Login = 1,
    Movement = 2,
    Chat = 3,
    WorldState = 4,
    Action = 5
}

public abstract class NetworkPacket
{
    public PacketType PacketType { get; set; }
    public int PlayerId { get; set; }
    
    public static string ToJson(NetworkPacket packet) => JsonSerializer.Serialize(packet, packet.GetType());
    public static NetworkPacket? FromJson(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        PacketType type = Enum.Parse<PacketType>(doc.RootElement.GetProperty("PacketType").GetInt32().ToString());
        return type switch
        {
            PacketType.Login => JsonSerializer.Deserialize<LoginPacket>(json),
            PacketType.Movement => JsonSerializer.Deserialize<MovementPacket>(json),
            PacketType.Chat => JsonSerializer.Deserialize<ChatPacket>(json),
            PacketType.WorldState => JsonSerializer.Deserialize<WorldPacket>(json),
            PacketType.Action => JsonSerializer.Deserialize<WorldPacket>(json),
            _ => null
        };
    }
}

public class LoginPacket : NetworkPacket
{
    public string PlayerName { get; set; }
    
    public LoginPacket(string playerName, int playerId)
    {
        PacketType = PacketType.Login;
        PlayerName = playerName;
        PlayerId = playerId;
    }
}

public class MovementPacket : NetworkPacket
{
    public Location MovementVector { get; set; }
    
    public MovementPacket(Location movementVector, int playerId)
    {
        PacketType = PacketType.Movement;
        MovementVector = movementVector;
        PlayerId = playerId;
    }
}
public class ChatPacket : NetworkPacket
{
    public string Message { get; set; }

    public ChatPacket(string message, int playerId)
    {
        PacketType = PacketType.Chat;
        Message = message;
        PlayerId = playerId;
    }

    public ChatPacket()
    {
        PacketType = PacketType.Chat;
        Message = "";
    }
}

public class WorldPacket : NetworkPacket
{
    public ConcurrentDictionary<int, Player> Players { get; set; }

    public WorldPacket(ConcurrentDictionary<int, Player> players)
    {
        PacketType = PacketType.WorldState;
        Players = players;
    }
}

public class ActionPacket : NetworkPacket
{
    
}
