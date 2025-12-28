using System.Collections.Concurrent;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class ServerPackageManager
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
                //InitializePlayerInformation(loginPacket);
                break;
            case PacketType.Movement:
                MovementPacket movementPacket = (MovementPacket)packet;
                HandleMovement(movementPacket, world);
                break;
            case PacketType.Chat:
                ChatPacket chatPacket = (ChatPacket)packet;
                //DisplayChatMessage(chatPacket);
                break;
            case PacketType.Action:
                ActionPacket actionPacket = (ActionPacket)packet;
                HandleAction(actionPacket, world);
                break;
        }
    }

    private void HandleMovement(MovementPacket packet, World world)
    {
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null)
        {
            player.Position = packet.MovementVector;
        }
    }
    private void HandleAction(ActionPacket packet, World world)
    {
        Player? player = world.GetPlayer(packet.PlayerId);
        if (player != null)
        {
            // Process action based on ActionType
            Console.WriteLine($"Player {player.Name} performed action {packet.ProjectileType}");

            Projectile projectile = EntityManager.CreateProjectile(packet.PlayerId, packet.Direction, 2, 100, packet.ProjectileType);
            projectile.Position = player.Position + packet.Direction;
        }
    }
}