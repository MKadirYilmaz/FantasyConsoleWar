using System.Collections.Concurrent;
using FantasyWar_Server;
using FantasyWar_Engine;


class Program
{
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
            }
            
            foreach (Entity entity in serverWorld.Entities.Values)
            {
                if (entity is Player or Projectile)
                {
                    MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                    server.UdpServer?.BroadcastPacket(moveUpdate);
                }
            }
            var playersDict = new ConcurrentDictionary<int, Player>(
                serverWorld.Entities.Values.OfType<Player>().ToDictionary(p => p.Id, p => p)
            );
            var projDict = new ConcurrentDictionary<int, Projectile>(
                serverWorld.Entities.Values.OfType<Projectile>().ToDictionary(p => p.Id, p => p)
            );
                
            var entitiesDict = new ConcurrentDictionary<int, Entity>(
                serverWorld.Entities.Values.Where(e => !(e is Player) && !(e is Projectile)).ToDictionary(e => e.Id, e => e)
            );
        
            WorldPacket worldPacket = new WorldPacket(entitiesDict, playersDict, projDict);
            //server.UdpServer?.BroadcastPacket(worldPacket);
            
            Thread.Sleep(20); // 50 FPS
        }
    }
}




