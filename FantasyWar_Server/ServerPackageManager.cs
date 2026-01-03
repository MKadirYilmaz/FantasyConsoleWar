using System.Collections.Concurrent;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class ServerPackageManager
{
    private ConcurrentQueue<NetworkPacket> _packetQueue = new ConcurrentQueue<NetworkPacket>();
    public ConcurrentQueue<NetworkPacket?> PacketSendQueue = new ConcurrentQueue<NetworkPacket?>();
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
                //InitializePlayerInformation(loginPacket);
                break;
            case PacketType.Movement:
                MovementPacket movementPacket = (MovementPacket)packet;
                HandleMovement(movementPacket, world);
                break;
            case PacketType.Chat:
                ChatPacket chatPacket = (ChatPacket)packet;
                PacketSendQueue.Enqueue(chatPacket);
                Console.WriteLine($"[Chat] Player {chatPacket.PlayerId}: {chatPacket.Message}");
                break;
            case PacketType.Action:
                ActionPacket actionPacket = (ActionPacket)packet;
                HandleAction(actionPacket, world);
                break;
            case PacketType.PlayerReady:
                PlayerReadyPacket readyPacket = (PlayerReadyPacket)packet;
                HandlePlayerReady(readyPacket, world);
                break;
            case PacketType.UpdatePlayerInfo:
                UpdatePlayerInfoPacket infoPacket = (UpdatePlayerInfoPacket)packet;
                HandleUpdatePlayerInfo(infoPacket, world);
                break;
        }
    }

    private void HandlePlayerReady(PlayerReadyPacket packet, World world)
    {
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null)
        {
            player.IsReady = packet.IsReady;
        }
    }

    private void HandleUpdatePlayerInfo(UpdatePlayerInfoPacket packet, World world)
    {
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null)
        {
            player.Name = packet.Name;
            player.SetVisual(packet.Visual);
        }
    }

    private void HandleMovement(MovementPacket packet, World world)
    {
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null && player.CanMove && player.AllowedToMove())
        {
            if (player.SetActorLocation(packet.MovementVector))
                player.OnMove();
        }
    }
    private void HandleAction(ActionPacket packet, World world)
    {
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null)
        {
            if (!player.AbilitySystem.CanUseAbility(packet.ProjectileType))
                return;

            player.AbilitySystem.UseAbility(packet.ProjectileType);

            // Process action based on ActionType
            Console.WriteLine($"Player {player.Name} performed action {packet.ProjectileType}");

            Projectile projectile = EntityManager.CreateProjectile(packet.PlayerId, packet.Direction, 2, 100, packet.ProjectileType);
            projectile.SetActorLocation(player.GetActorLocation() + packet.Direction);
            projectile.IsSolid = false;
            
            // Enqueue a packet to notify clients about the new projectile
            SpawnOrDestroyProjectilePacket spawnPacket = new SpawnOrDestroyProjectilePacket(projectile, true);
            PacketSendQueue.Enqueue(spawnPacket);
        }
    }
}