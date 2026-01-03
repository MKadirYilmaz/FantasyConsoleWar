using System.Collections.Concurrent;
using FantasyWar_Engine;

namespace FantasyWar_Client;

public class ClientPackageHandler
{
    private ConcurrentQueue<NetworkPacket> _packetQueue = new ConcurrentQueue<NetworkPacket>();
    
    public List<LobbyPlayerData> LobbyPlayers { get; private set; } = new();
    public bool GameStarted { get; private set; }
    public bool IsGameOver { get; private set; }
    public List<LobbyPlayerData> Rankings { get; private set; } = new();
    public bool BackToLobby { get; private set; }

    public int SafeMinX { get; private set; }
    public int SafeMaxX { get; private set; } = 100;
    public int SafeMinY { get; private set; }
    public int SafeMaxY { get; private set; } = 100;

    public event Action<int>? OnPlayerLogin;

    public void Reset()
    {
        GameStarted = false;
        IsGameOver = false;
        Rankings.Clear();
        BackToLobby = false;
        LobbyPlayers.Clear();
    }

    public void OnDataReceived(NetworkPacket? packet)
    {
        if(packet != null)
            _packetQueue.Enqueue(packet);
    }
    
    public void ProcessPackets(ClientGameState gameState)
    {
        while (_packetQueue.TryDequeue(out var packet))
        {
            HandlePacket(packet, gameState);
        }
    }
    
    private void HandlePacket(NetworkPacket? packet, ClientGameState gameState)
    {
        if (packet == null) return;
        World world = gameState.World;
        //Console.WriteLine($"Received: {packet.GetType().Name}");

        switch (packet.PacketType)
        {
            case PacketType.Login:
                LoginPacket loginPacket = (LoginPacket)packet;
                HandleLogin(loginPacket, gameState);
                break;
            case PacketType.Movement:
                MovementPacket movementPacket = (MovementPacket)packet;
                HandleMovement(movementPacket, world);
                break;
            case PacketType.Chat:
                ChatPacket chatPacket = (ChatPacket)packet;
                HandleChat(chatPacket, world);
                break;
            case PacketType.WorldState:
                WorldPacket worldPacket = (WorldPacket)packet;
                HandleWorldStateUpdate(worldPacket, gameState);
                break;
            case PacketType.SpawnOrDestroyPlayer:
                SpawnOrDestroyPlayerPacket spawnPacket = (SpawnOrDestroyPlayerPacket)packet;
                HandleSpawnOrDestroyPlayer(spawnPacket, world);
                break;
            case PacketType.SpawnOrDestroyProjectile:
                SpawnOrDestroyProjectilePacket projPacket = (SpawnOrDestroyProjectilePacket)packet;
                HandleSpawnOrDestroyProjectile(projPacket, world);
                break;
            case PacketType.PlayerStatus:
                PlayerStatusPacket statusPacket = (PlayerStatusPacket)packet;
                HandlePlayerStatus(statusPacket, world);
                break;
            case PacketType.LobbyState:
                LobbyStatePacket lobbyPacket = (LobbyStatePacket)packet;
                LobbyPlayers = lobbyPacket.Players;
                UpdateWorldFromLobbyState(lobbyPacket, world);
                if (GameStarted) BackToLobby = true; // If we receive lobby state while game is started, it means we are back to lobby
                break;
            case PacketType.GameStart:
                GameStarted = true;
                break;
            case PacketType.GameOver:
                GameOverPacket gameOverPacket = (GameOverPacket)packet;
                IsGameOver = true;
                Rankings = gameOverPacket.Rankings;
                break;
            case PacketType.RingState:
                RingStatePacket ringPacket = (RingStatePacket)packet;
                SafeMinX = ringPacket.SafeMinX;
                SafeMaxX = ringPacket.SafeMaxX;
                SafeMinY = ringPacket.SafeMinY;
                SafeMaxY = ringPacket.SafeMaxY;
                break;
        }
    }
    
    
    private void HandleLogin(LoginPacket packet, ClientGameState gameState)
    {
        World world = gameState.World;
        Console.WriteLine($"[Login] Player: {packet.PlayerName}, ID: {packet.PlayerId}");
        
        var newPlayer = new Player(packet.PlayerId, packet.PlayerName, packet.SpawnLocation);
        
        if (gameState.LocalPlayerId == -1)
        {
            FantasyWar_Client.PlayerCamera camera = new FantasyWar_Client.PlayerCamera();
            camera.FollowPlayer(newPlayer.Id);
            gameState.LocalCamera = camera;
            
            gameState.LocalPlayerId = packet.PlayerId;
            newPlayer.IsLocalPlayer = true;
            
            Console.WriteLine("-> This is ME!");
            OnPlayerLogin?.Invoke(packet.PlayerId);
        }
        
        world.AddOrUpdateEntity(packet.PlayerId, newPlayer);
    }

    private void HandleWorldStateUpdate(WorldPacket packet, ClientGameState gameState)
    {
        World world = gameState.World;
        // 1) Upsert everything we received \-\- World.AddOrUpdateEntity updates grids consistently
        foreach (var kvp in packet.Players)
        {
            int playerId = kvp.Key;
            Player incomingPlayer = kvp.Value;

            if (playerId == gameState.LocalPlayerId) incomingPlayer.IsLocalPlayer = true;
            if (!incomingPlayer.IsWaiting) incomingPlayer.IsSolid = true;

            world.AddOrUpdateEntity(playerId, incomingPlayer);
        }

        foreach (var kvp in packet.Projectiles)
        {
            Projectile proj = kvp.Value;
            proj.IsSolid = false;
            world.AddOrUpdateEntity(kvp.Key, proj);
        }

        foreach (var kvp in packet.Entities)
        {
            if(kvp.Value is not Player or Projectile)
                world.AddOrUpdateEntity(kvp.Key, kvp.Value);
        }

        // 2) Remove ids that no longer exist in the packet \-\- World.RemoveEntity also clears grids
        foreach (var localId in world.Entities.Keys)
        {
            bool existsInPacket =
                packet.Players.ContainsKey(localId) ||
                packet.Entities.ContainsKey(localId) ||
                packet.Projectiles.ContainsKey(localId);

            if (!existsInPacket)
            {
                world.RemoveEntity(localId);
            }
        }
        
    }

    private void HandleMovement(MovementPacket packet, World world)
    {
        world.Entities.TryGetValue(packet.PlayerId, out Entity? entity);
        
        entity?.SetActorLocation(packet.MovementVector, world);
    }

    private void HandleChat(ChatPacket packet, World world)
    {
        var player = world.GetPlayer(packet.PlayerId);
        string name = player != null ? player.Name : "Unknown";

        string message = $"[Chat] {name}: {packet.Message}";
        world.ChatMessages.Add(message);
        if (world.ChatMessages.Count > 3)
        {
            world.ChatMessages.RemoveAt(0);
        }
    }
    
    private void HandleSpawnOrDestroyPlayer(SpawnOrDestroyPlayerPacket packet, World world)
    {
        if (packet.IsSpawn)
        {
            world.AddOrUpdateEntity(packet.SpawnedPlayer.Id, packet.SpawnedPlayer);
        }
        else
        {
            world.RemoveEntity(packet.SpawnedPlayer.Id);
        }
    }

    private void HandleSpawnOrDestroyProjectile(SpawnOrDestroyProjectilePacket packet, World world)
    {
        if (packet.IsSpawn)
        {
            Projectile proj = packet.SpawnedProjectile;
            proj.IsSolid = false;
            world.AddOrUpdateEntity(packet.SpawnedProjectile.Id, proj);
        }
        else
        {
            world.RemoveEntity(packet.SpawnedProjectile.Id);
        }
    }

    private void HandlePlayerStatus(PlayerStatusPacket packet, World world)
    {
        //Console.WriteLine($"[Status] ID:{packet.PlayerId} HP:{packet.Health}");
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null)
        {
            player.Health = packet.Health;
            player.Resistance = packet.Resistance;
            player.CanMove = packet.CanMove;
            player.IsBurning = packet.IsBurning;
        }
    }

    private void UpdateWorldFromLobbyState(LobbyStatePacket packet, World world)
    {
        foreach (var lobbyPlayer in packet.Players)
        {
            Player? player = world.GetPlayer(lobbyPlayer.Id);
            if (player != null)
            {
                player.Name = lobbyPlayer.Name;
                player.SetVisual(lobbyPlayer.Visual);
                player.IsReady = lobbyPlayer.IsReady;
            }
        }
    }

}