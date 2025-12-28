using FantasyWar_Server;
using FantasyWar_Engine;


class Program
{
    static void Main(string[] args)
    {
        World serverWorld = new World(50, 50);
        
        PhysicsSystem physicsSystem = new PhysicsSystem();
        
        ServerPackageManager serverPackageManager = new ServerPackageManager();
        Server server = new Server();
        server.Start(5000, 5001, serverPackageManager, serverWorld);
        
        Console.WriteLine("Server started on port 5000 and UDP target port 5001.");

        
        while (true)
        {
            serverPackageManager.ProcessPackets(serverWorld);

            physicsSystem.Update(serverWorld, 0.02f); // Assuming 50 FPS, so deltaTime is 0.02 seconds
            
            foreach (Player player in serverWorld.Players.Values)
            {
                MovementPacket moveUpdate = new MovementPacket(player.Position, player.Id);
                server.UdpServer?.BroadcastPacket(moveUpdate);
                
                WorldPacket worldPacket = new WorldPacket(serverWorld.Players, serverWorld.Entities);
                server.UdpServer?.BroadcastPacket(worldPacket);
            }
            Thread.Sleep(20); // 50 FPS
        }
    }
}




