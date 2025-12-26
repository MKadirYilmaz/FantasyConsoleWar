using FantasyWar_Server;
using FantasyWar_Engine;


class Program
{
    static void Main(string[] args)
    {
        World serverWorld = new World(100, 100);
        
        ServerPackageManager serverPackageManager = new ServerPackageManager();
        Server server = new Server();
        server.Start(5000, 5001, serverPackageManager, serverWorld);
        
        Console.WriteLine("Server started on port 5000 and UDP target port 5001.");

        while (true)
        {
            serverPackageManager.ProcessPackets(serverWorld);

            foreach (Player player in serverWorld.Players.Values)
            {
                MovementPacket moveUpdate = new MovementPacket(player.Position, player.Id);
                server.UdpServer?.BroadcastPacket(moveUpdate);
                
            }
            Thread.Sleep(20); // 50 FPS
        }
    }
}




