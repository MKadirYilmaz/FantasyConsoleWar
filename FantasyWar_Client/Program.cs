using FantasyWar_Client;
using FantasyWar_Engine;

class Program
{
    private static Client? _client;
    private static Vector _lastDirecition = new Vector(0, 1);
    private static ProjectileType _projectileType = ProjectileType.Physical;
    
    private const int TARGET_FRAME_RATE = 60;
    
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Title = "Fantasy Console Client";
        
        World gameWorld = new World(50, 50);
        ClientPackageHandler packageHandler = new ClientPackageHandler();
        
        Console.WriteLine("Connecting to server...");
        _client = new Client("127.0.0.1", 5000, 5001, packageHandler);
        
        while (gameWorld.LocalPlayerId == -1)
        {
            Console.WriteLine("Waiting for server to assign player ID...");
            packageHandler.ProcessPackets(gameWorld);
            Thread.Sleep(100);
        }
        int viewHeight = gameWorld.LocalCamera == null ? 20 : gameWorld.LocalCamera.ViewHeight;
        int viewWidth = gameWorld.LocalCamera == null ? 50 : gameWorld.LocalCamera.ViewWidth;
        RenderSystem renderSystem = new RenderSystem(viewWidth, viewHeight);
        
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
            
            Render(gameWorld, renderSystem);
            
            Thread.Sleep(1000 / TARGET_FRAME_RATE);
        }

    }
    static void HandleInput(ConsoleKey key, World world)
    {
        Player? localPlayer = world.GetPlayer(world.LocalPlayerId);
        if (localPlayer == null) return;

        Vector movement = new Vector(0, 0);
        switch (key)
        {
            case ConsoleKey.W:
                movement = new Vector(0, -1);
                _lastDirecition = new Vector(0, -1);
                break;
            case ConsoleKey.S:
                movement = new Vector(0, 1);
                _lastDirecition = new Vector(0, 1);
                break;
            case ConsoleKey.A:
                movement = new Vector(-1, 0);
                _lastDirecition = new Vector(-1, 0);
                break;
            case ConsoleKey.D:
                movement = new Vector(1, 0);
                _lastDirecition = new Vector(1, 0);
                break;
            case ConsoleKey.D1:
                _projectileType = ProjectileType.Physical;
                break;
            case ConsoleKey.D2:
                _projectileType = ProjectileType.Electric;
                break;
            case ConsoleKey.D3:
                _projectileType = ProjectileType.Fire;
                break;
            case ConsoleKey.D4:
                _projectileType = ProjectileType.Ice;
                break;
            case ConsoleKey.Spacebar:
                ActionPacket actionPacket = new ActionPacket(_projectileType, world.LocalPlayerId, _lastDirecition);
                _client?.TcpClient.SendPacket(actionPacket);
                break;
        }
        
        

        if (movement.X != 0 || movement.Y != 0)
        {
            //localPlayer.AddActorPosition(movement, world);

            MovementPacket movePacket = new MovementPacket(localPlayer.GetActorLocation() + movement, localPlayer.Id);
            
            _client?.TcpClient.SendPacket(movePacket);
        }
    }

    static void Render(World world, RenderSystem renderSystem)
    {
        if (world.LocalCamera == null) return;
        renderSystem.Render(world, world.LocalCamera);
    }
}