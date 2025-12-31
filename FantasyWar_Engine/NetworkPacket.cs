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
    SpawnOrDestroyProjectile = 7,
    PlayerStatus = 8,
    LobbyState = 9,
    PlayerReady = 10,
    UpdatePlayerInfo = 11,
    GameStart = 12,
    GameOver = 13,
    RingState = 14
}

public class RingStatePacket : NetworkPacket
{
    public int SafeMinX { get; set; }
    public int SafeMaxX { get; set; }
    public int SafeMinY { get; set; }
    public int SafeMaxY { get; set; }

    public RingStatePacket()
    {
        PacketType = PacketType.RingState;
    }

    public RingStatePacket(int safeMinX, int safeMaxX, int safeMinY, int safeMaxY)
    {
        PacketType = PacketType.RingState;
        SafeMinX = safeMinX;
        SafeMaxX = safeMaxX;
        SafeMinY = safeMinY;
        SafeMaxY = safeMaxY;
    }
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
            PacketType.PlayerStatus => JsonSerializer.Deserialize<PlayerStatusPacket>(json),
            PacketType.LobbyState => JsonSerializer.Deserialize<LobbyStatePacket>(json),
            PacketType.PlayerReady => JsonSerializer.Deserialize<PlayerReadyPacket>(json),
            PacketType.UpdatePlayerInfo => JsonSerializer.Deserialize<UpdatePlayerInfoPacket>(json),
            PacketType.GameStart => JsonSerializer.Deserialize<GameStartPacket>(json),
            PacketType.GameOver => JsonSerializer.Deserialize<GameOverPacket>(json),
            PacketType.RingState => JsonSerializer.Deserialize<RingStatePacket>(json),
            _ => null
        };
    }
}

public class LobbyPlayerData
{
    public int Id { get; set; }
    public string Name { get; set; } = "Unknown";
    public string Visual { get; set; } = "?";
    public bool IsReady { get; set; }
}

public class GameOverPacket : NetworkPacket
{
    public List<LobbyPlayerData> Rankings { get; set; }

    public GameOverPacket(List<LobbyPlayerData> rankings)
    {
        PacketType = PacketType.GameOver;
        Rankings = rankings;
    }
}

public class LobbyStatePacket : NetworkPacket
{
    public List<LobbyPlayerData> Players { get; set; }

    public LobbyStatePacket(List<LobbyPlayerData> players)
    {
        PacketType = PacketType.LobbyState;
        Players = players;
    }
}

public class PlayerReadyPacket : NetworkPacket
{
    public bool IsReady { get; set; }

    public PlayerReadyPacket(int playerId, bool isReady)
    {
        PacketType = PacketType.PlayerReady;
        PlayerId = playerId;
        IsReady = isReady;
    }
}

public class UpdatePlayerInfoPacket : NetworkPacket
{
    public string Name { get; set; }
    public string Visual { get; set; }

    public UpdatePlayerInfoPacket(int playerId, string name, string visual)
    {
        PacketType = PacketType.UpdatePlayerInfo;
        PlayerId = playerId;
        Name = name;
        Visual = visual;
    }
}

public class GameStartPacket : NetworkPacket
{
    public GameStartPacket()
    {
        PacketType = PacketType.GameStart;
    }
}

public class PlayerStatusPacket : NetworkPacket
{
    public int Health { get; set; }
    public int Resistance { get; set; }
    public bool CanMove { get; set; }
    public bool IsBurning { get; set; }

    public PlayerStatusPacket(int playerId, int health, int resistance, bool canMove, bool isBurning)
    {
        PacketType = PacketType.PlayerStatus;
        PlayerId = playerId;
        Health = health;
        Resistance = resistance;
        CanMove = canMove;
        IsBurning = isBurning;
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
