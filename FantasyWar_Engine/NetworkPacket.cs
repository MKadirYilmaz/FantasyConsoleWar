using System.Collections.Concurrent;
using System.Text.Json;

namespace FantasyWar_Engine;

public enum PacketType : byte
{
    Login = 1,
    Movement = 2,
    Chat = 3,
    WorldState = 4,
    Action = 5,
    SpawnOrDestroyPlayer = 6,
    SpawnOrDestroyProjectile = 7
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
            PacketType.Action => JsonSerializer.Deserialize<ActionPacket>(json),
            PacketType.SpawnOrDestroyPlayer => JsonSerializer.Deserialize<SpawnOrDestroyPlayerPacket>(json),
            PacketType.SpawnOrDestroyProjectile => JsonSerializer.Deserialize<SpawnOrDestroyProjectilePacket>(json),
            _ => null
        };
    }
}

public class LoginPacket : NetworkPacket
{
    public string PlayerName { get; set; }
    public Vector SpawnLocation { get; set; }
    
    public LoginPacket(string playerName, int playerId, Vector spawnLocation)
    {
        PacketType = PacketType.Login;
        PlayerName = playerName;
        PlayerId = playerId;
        SpawnLocation = spawnLocation;
    }
}

public class MovementPacket : NetworkPacket
{
    public Vector MovementVector { get; set; }
    
    public MovementPacket(Vector movementVector, int playerId)
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
    public ConcurrentDictionary<int, Entity> Entities { get; set; }
    public ConcurrentDictionary<int, Player> Players { get; set; }
    public ConcurrentDictionary<int, Projectile> Projectiles { get; set; }

    public WorldPacket(ConcurrentDictionary<int, Entity> entities, ConcurrentDictionary<int, Player> players, ConcurrentDictionary<int, Projectile> projectiles)
    {
        PacketType = PacketType.WorldState;
        Entities = entities;
        Players = players;
        Projectiles = projectiles;
    }
}

public class ActionPacket : NetworkPacket
{
    public ProjectileType ProjectileType { get; set; }
    public Vector Direction { get; set; }
    
    public ActionPacket(ProjectileType projectileType, int playerId, Vector direction)
    {
        PacketType = PacketType.Action;
        ProjectileType = projectileType;
        PlayerId = playerId;
        Direction = direction;
    }
}

public class SpawnOrDestroyPlayerPacket : NetworkPacket
{
    public Player SpawnedPlayer { get; set; }
    public bool IsSpawn { get; set; }

    public SpawnOrDestroyPlayerPacket(Player spawnedPlayer, bool isSpawn)
    {
        PacketType = PacketType.SpawnOrDestroyPlayer;
        SpawnedPlayer = spawnedPlayer;
        IsSpawn = isSpawn;
    }
}

public class SpawnOrDestroyProjectilePacket : NetworkPacket
{
    public Projectile SpawnedProjectile { get; set; }
    public bool IsSpawn { get; set; }

    public SpawnOrDestroyProjectilePacket(Projectile spawnedProjectile, bool isSpawn)
    {
        PacketType = PacketType.SpawnOrDestroyProjectile;
        SpawnedProjectile = spawnedProjectile;
        IsSpawn = isSpawn;
    }
}
