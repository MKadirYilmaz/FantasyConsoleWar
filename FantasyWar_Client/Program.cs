using FantasyWar_Client;
using FantasyWar_Engine;

namespace FantasyWar_Client;

class Program
{
    private static Client? _client;
    
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Title = "Fantasy Console Client";
        
        World gameWorld = new World(50, 50);
        ClientGameState gameState = new ClientGameState(gameWorld);
        ClientPackageHandler packageHandler = new ClientPackageHandler();
        
        Console.Write("Enter Server IP (default 127.0.0.1): ");
        string? ipInput = Console.ReadLine();
        string serverIp = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput;

        Console.WriteLine($"Connecting to server at {serverIp}...");
        _client = new Client(serverIp, 5000, 0, packageHandler);
        
        while (gameState.LocalPlayerId == -1)
        {
            Console.WriteLine("Waiting for server to assign player ID...");
            packageHandler.ProcessPackets(gameState);
            Thread.Sleep(100);
        }
        
        GameClient gameClient = new GameClient(_client, gameState, packageHandler);
        gameClient.Run();
    }
}

