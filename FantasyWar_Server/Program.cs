using System.Net;
using System.Net.Sockets;
using FantasyWar_Engine;

namespace FantasyWar_Server;

class Program
{
    static void Main()
    {
        World serverWorld = new World(50, 50, true);
        
        ServerPackageManager serverPackageManager = new ServerPackageManager();
        Server server = new Server();
        server.Start(5000, 5001, serverPackageManager);
        
        Console.WriteLine("Server started on port 5000 and UDP target port 5001.");

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Console.WriteLine($"Server IP Address: {ip}");
            }
        }
        
        GameLogic gameLogic = new GameLogic(serverWorld, server, serverPackageManager);
        gameLogic.Run();
    }
}












