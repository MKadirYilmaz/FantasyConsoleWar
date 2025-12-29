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
        
        world.AddOrUpdatePlayer(packet.PlayerId, newPlayer);
        world.AddOrUpdateEntity(packet.PlayerId, newPlayer);
    }

    private void HandleWorldStateUpdate(WorldPacket packet, World world)
    {
        // Add or update players
        foreach(var kvp in packet.Players)
        {
            int playerId = kvp.Key;
            Player incomingPlayer = kvp.Value;
            
            
            if (playerId == world.LocalPlayerId)
            {
                incomingPlayer.IsLocalPlayer = true;
            }
            
            world.AddOrUpdatePlayer(playerId, incomingPlayer);
            world.AddOrUpdateEntity(playerId, incomingPlayer);
        }

        foreach (var kvp in world.Players)
        {
            int id = kvp.Key;
            
            if (!packet.Players.ContainsKey(id))
            {
                world.Players.TryRemove(id, out _);
                world.Entities.TryRemove(id, out _);
                Console.WriteLine("[System] Player removed: " + kvp.Value.Name);
            }
        }
        
        if (!packet.Entities.IsEmpty)
        {
            // Add or update entities
            foreach (var kvp in packet.Entities)
            {
                int entityId = kvp.Key;
                Entity incomingEntity = kvp.Value;

                // Skip players, already handled
                if (world.Players.ContainsKey(entityId)) continue;

                world.AddOrUpdateEntity(entityId, incomingEntity);
            }

            // Remove entities that are no longer present
            foreach (var localEntityId in world.Entities.Keys)
            {
                // Skip players, already handled
                if (world.Players.ContainsKey(localEntityId)) continue;

                if (!packet.Entities.ContainsKey(localEntityId))
                {
                    world.Entities.TryRemove(localEntityId, out _);
                }
            }
        }
    }

    private void HandleMovement(MovementPacket packet, World world)
    {
        var player = world.GetPlayer(packet.PlayerId);

        if (player != null)
        {
            
            player.SetActorLocation(packet.MovementVector);
            
            // Console.WriteLine($"Player {player.Name} moved to {player.Position}");
        }
        else
        {
            Console.WriteLine($"[Warning] Received move for unknown player: {packet.PlayerId}");
        }
    }

    private void HandleChat(ChatPacket packet, World world)
    {
        var player = world.GetPlayer(packet.PlayerId);
        string name = player != null ? player.Name : "Unknown";

        Console.WriteLine($"[Chat] {name}: {packet.Message}");
    }
    
    
}