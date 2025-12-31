using System.Collections.Concurrent;
using FantasyWar_Server;
using FantasyWar_Engine;


class Program
{
    private const int TARGET_FRAME_RATE = 60;
    
    static void Main(string[] args)
    {
        World serverWorld = new World(50, 50, true);
        
        PhysicsSystem physicsSystem = new PhysicsSystem();
        
        ServerPackageManager serverPackageManager = new ServerPackageManager();
        Server server = new Server();
        server.Start(5000, 5001, serverPackageManager, serverWorld);
        
        Console.WriteLine("Server started on port 5000 and UDP target port 5001.");

        
        while (true)
        {
            serverPackageManager.ProcessPackets(serverWorld);

            while (serverPackageManager.PacketSendQueue.TryDequeue(out var packet))
            {
                if (packet is SpawnOrDestroyPlayerPacket spawnOrDestroyPlayerPacket)
                {
                    server.TcpServer?.BroadcastPacket(spawnOrDestroyPlayerPacket);
                }
                else if (packet is SpawnOrDestroyProjectilePacket spawnOrDestroyProjectilePacket)
                {
                    server.TcpServer?.BroadcastPacket(spawnOrDestroyProjectilePacket);
                }
            }

            var destroyedEntities = physicsSystem.Update(serverWorld, 0.02f); // Assuming 50 FPS, so deltaTime is 0.02 seconds

            foreach (var entity in destroyedEntities)
            {
                if (entity is Projectile projectile)
                {
                    SpawnOrDestroyProjectilePacket destroyPacket = new SpawnOrDestroyProjectilePacket(projectile, false);
                    server.TcpServer?.BroadcastPacket(destroyPacket);
                }
                else if (entity is Player player)
                {
                    SpawnOrDestroyPlayerPacket destroyPacket = new SpawnOrDestroyPlayerPacket(player, false);
                    server.TcpServer?.BroadcastPacket(destroyPacket);
                }
            }
            
            foreach (Entity entity in serverWorld.Entities.Values)
            {
                if (entity is Player player)
                {
                    MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                    server.UdpServer?.BroadcastPacket(moveUpdate);
                    
                    PlayerStatusPacket statusPacket = new PlayerStatusPacket(player.Id, player.Health, player.Resistance, player.CanMove, player.IsBurning);
                    server.UdpServer?.BroadcastPacket(statusPacket);
                }
                else if (entity is Projectile)
                {
                    MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                    server.UdpServer?.BroadcastPacket(moveUpdate);
                }
            }
            Thread.Sleep(1000 / TARGET_FRAME_RATE);
        }
    }
}




