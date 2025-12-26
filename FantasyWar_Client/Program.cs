using FantasyWar_Client;
using FantasyWar_Engine;

class Program
{
    private static Client client;
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Title = "Fantasy Console Client";
        
        World gameWorld = new World(100, 100);
        ClientPackageHandler packageHandler = new ClientPackageHandler();
        
        Console.WriteLine("Connecting to server...");
        client = new Client("127.0.0.1", 5000, 5001, packageHandler);
        
        while (gameWorld.LocalPlayerId == -1)
        {
            packageHandler.ProcessPackets(gameWorld);
            Console.WriteLine("Waiting for server to assign player ID...");
            Thread.Sleep(100);
        }
        
        bool isRunning = true;
        while (isRunning)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                
                if(key == ConsoleKey.Escape) isRunning = false;
                
                HandleInput(key, gameWorld);
            }
            
            packageHandler.ProcessPackets(gameWorld);
            
            Render(gameWorld);
            
            Thread.Sleep(20); // 50 FPS
        }

    }
    static void HandleInput(ConsoleKey key, World world)
    {
        Player? localPlayer = world.GetPlayer(world.LocalPlayerId);
        if (localPlayer == null) return;

        Location movement = key switch
        {
            ConsoleKey.W => new Location(0, -1),
            ConsoleKey.S => new Location(0, 1),
            ConsoleKey.A => new Location(-1, 0),
            ConsoleKey.D => new Location(1, 0),
            _ => new Location(0, 0)
        };

        if (movement.X != 0 || movement.Y != 0)
        {
            localPlayer.AddActorPosition(movement, world);

            MovementPacket movePacket = new MovementPacket(localPlayer.Position, localPlayer.Id);
            
            client.TcpClient.SendPacket(movePacket);
        }
    }

    static void Render(World world)
    {
        // Use double buffering later
        Console.Clear();

        // UI
        Console.SetCursorPosition(0, 0);
        Console.WriteLine($"Players Online: {world.Players.Count} | [WASD] Move | [ESC] Quit");
        
        world.LocalCamera?.DrawView(world);
    }
}