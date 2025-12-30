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
        // 1. Gelen Player listesini Entities'e işle
        foreach(var kvp in packet.Players)
        {
            int playerId = kvp.Key;
            Player incomingPlayer = kvp.Value;
            
            if (playerId == world.LocalPlayerId) incomingPlayer.IsLocalPlayer = true;
            
            world.AddOrUpdateEntity(playerId, incomingPlayer);
        }
        
        // 2. Gelen Projectile'leri Entities'e işle
        foreach (var kvp in packet.Projectiles)
        {
            world.AddOrUpdateEntity(kvp.Key, kvp.Value);
        }
        
        // 3. Gelen diğer Entity'leri Entities'e işle
        foreach (var kvp in packet.Entities)
        {
            world.AddOrUpdateEntity(kvp.Key, kvp.Value);
        }
        
        // 3. Silinenleri Temizle (Hem Player hem Entity için tek döngü yeterli değil, paketteki her şeyi kontrol etmeliyiz)
        // Basit yöntem: Pakette olmayanları sil.
        foreach (var localId in world.Entities.Keys)
        {
            bool existsInPacket = packet.Players.ContainsKey(localId) || packet.Entities.ContainsKey(localId) || packet.Projectiles.ContainsKey(localId);
            
            if (!existsInPacket)
            {
                world.Entities.TryRemove(localId, out _);
            }
        }

        
        // 4. Grid Senkronizasyonu
        for (int x = 0; x < world.Width; x++)
        for (int y = 0; y < world.Height; y++)
            world.Grid[x, y] = -1;
        
        foreach (var entity in world.Entities.Values)
        {
            Vector pos = entity.GetActorLocation();
            if(pos.X >= 0 && pos.X < world.Width && pos.Y >= 0 && pos.Y < world.Height)
                world.Grid[pos.X, pos.Y] = entity.Id;
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

        Console.WriteLine($"[Chat] {name}: {packet.Message}");
    }
    
    private void HandleSpawnOrDestroyPlayer(SpawnOrDestroyPlayerPacket packet, World world)
    {
        if (packet.IsSpawn)
        {
            world.AddOrUpdateEntity(packet.SpawnedPlayer.Id, packet.SpawnedPlayer);
        }
        else
        {
            world.Entities.TryRemove(packet.SpawnedPlayer.Id, out _);
        }
    }

    private void HandleSpawnOrDestroyProjectile(SpawnOrDestroyProjectilePacket packet, World world)
    {
        if (packet.IsSpawn)
        {
            world.AddOrUpdateEntity(packet.SpawnedProjectile.Id, packet.SpawnedProjectile);
        }
        else
        {
            world.Entities.TryRemove(packet.SpawnedProjectile.Id, out _);
        }
    }
}