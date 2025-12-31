using System.Collections.Concurrent;
using FantasyWar_Engine;

namespace FantasyWar_Client;

public class ClientPackageHandler
{
    private ConcurrentQueue<NetworkPacket> _packetQueue = new ConcurrentQueue<NetworkPacket>();
    
    public void OnDataReceived(NetworkPacket? packet)
    {
        if(packet != null)
            _packetQueue.Enqueue(packet);
    }
    
    public void ProcessPackets(World world)
    {
        while (_packetQueue.TryDequeue(out var packet))
        {
            HandlePacket(packet, world);
        }
    }
    
    private void HandlePacket(NetworkPacket? packet, World world)
    {
        if (packet == null) return;
        //Console.WriteLine($"Received: {packet.GetType().Name}");

        switch (packet.PacketType)
        {
            case PacketType.Login:
                LoginPacket loginPacket = (LoginPacket)packet;
                HandleLogin(loginPacket, world);
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
                HandleWorldStateUpdate(worldPacket, world);
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
                
        }
    }
    
    
    private void HandleLogin(LoginPacket packet, World world)
    {
        Console.WriteLine($"[Login] Player: {packet.PlayerName}, ID: {packet.PlayerId}");
        
        var newPlayer = new Player(packet.PlayerId, packet.PlayerName, packet.SpawnLocation);
        
        if (world.LocalPlayerId == -1)
        {
            PlayerCamera camera = new PlayerCamera();
            camera.FollowPlayer(newPlayer.Id);
            world.LocalCamera = camera;
            
            world.LocalPlayerId = packet.PlayerId;
            newPlayer.IsLocalPlayer = true;
            
            Console.WriteLine("-> This is ME!");
        }
        
        world.AddOrUpdateEntity(packet.PlayerId, newPlayer);
    }

    private void HandleWorldStateUpdate(WorldPacket packet, World world)
    {
        // 1) Upsert everything we received \-\- World.AddOrUpdateEntity updates grids consistently
        foreach (var kvp in packet.Players)
        {
            int playerId = kvp.Key;
            Player incomingPlayer = kvp.Value;

            if (playerId == world.LocalPlayerId) incomingPlayer.IsLocalPlayer = true;
            incomingPlayer.IsSolid = true;

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
        
        entity?.SetActorLocation(packet.MovementVector);
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

}