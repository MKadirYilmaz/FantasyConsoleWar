using FantasyWar_Client;
using FantasyWar_Engine;

class Program
{
    private static Client? _client;
    private static Vector _lastDirecition = new Vector(0, 1);
    private static ProjectileType _projectileType = ProjectileType.Physical;
    
    private static bool _isChatting = false;
    private static string _currentChatMessage = "";
    
    private const int TARGET_FRAME_RATE = 60;
    
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Title = "Fantasy Console Client";
        
        World gameWorld = new World(50, 50);
        ClientPackageHandler packageHandler = new ClientPackageHandler();
        
        Console.Write("Enter Server IP (default 127.0.0.1): ");
        string? ipInput = Console.ReadLine();
        string serverIp = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput;

        Console.WriteLine($"Connecting to server at {serverIp}...");
        _client = new Client(serverIp, 5000, 5001, packageHandler);
        
        while (gameWorld.LocalPlayerId == -1)
        {
            Console.WriteLine("Waiting for server to assign player ID...");
            packageHandler.ProcessPackets(gameWorld);
            Thread.Sleep(100);
        }
        
        while (true)
        {
            // Reset state for new game cycle
            packageHandler.Reset();
            
            // Start Main Menu
            MainMenu mainMenu = new MainMenu(_client, gameWorld, packageHandler);
            mainMenu.Run();
            
            int viewHeight = gameWorld.LocalCamera == null ? 20 : gameWorld.LocalCamera.ViewHeight;
            int viewWidth = gameWorld.LocalCamera == null ? 50 : gameWorld.LocalCamera.ViewWidth;
            RenderSystem renderSystem = new RenderSystem(viewWidth, viewHeight);
            
            bool isRunning = true;
            while (isRunning)
            {
                if (packageHandler.BackToLobby)
                {
                    isRunning = false;
                    continue;
                }

                if (packageHandler.IsGameOver)
                {
                    RenderWinScreen(packageHandler.Rankings);
                    packageHandler.ProcessPackets(gameWorld); // Keep processing to receive LobbyState/BackToLobby
                    Thread.Sleep(1000);
                    continue;
                }

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    ConsoleKey key = keyInfo.Key;
                    
                    if(key == ConsoleKey.Escape)
                    {
                        if (_isChatting)
                        {
                            _isChatting = false;
                            _currentChatMessage = "";
                        }
                        else
                        {
                            //isRunning = false; // Don't exit app, maybe just open menu? For now, disable exit
                        }
                    }
                    else if (_isChatting)
                    {
                        if (key == ConsoleKey.Enter)
                        {
                            if (!string.IsNullOrWhiteSpace(_currentChatMessage))
                            {
                                ChatPacket chatPacket = new ChatPacket(_currentChatMessage, gameWorld.LocalPlayerId);
                                _client?.TcpClient.SendPacket(chatPacket);
                                _currentChatMessage = "";
                            }
                            _isChatting = false;
                        }
                        else if (key == ConsoleKey.Backspace)
                        {
                            if (_currentChatMessage.Length > 0)
                            {
                                _currentChatMessage = _currentChatMessage.Substring(0, _currentChatMessage.Length - 1);
                            }
                        }
                        else
                        {
                            if (keyInfo.KeyChar >= ' ') // Ignore control chars
                            {
                                _currentChatMessage += keyInfo.KeyChar;
                            }
                        }
                    }
                    else
                    {
                        if (key == ConsoleKey.T)
                        {
                            _isChatting = true;
                            _currentChatMessage = "";
                        }
                        else
                        {
                            HandleInput(key, gameWorld);
                        }
                    }
                }
                
                packageHandler.ProcessPackets(gameWorld);
                
                if (!packageHandler.IsGameOver)
                {
                    Render(gameWorld, renderSystem, packageHandler);
                }
                
                Thread.Sleep(1000 / TARGET_FRAME_RATE);
            }
        }

    }

    static void RenderWinScreen(List<LobbyPlayerData> rankings)
    {
        Console.Clear();
        string winScreenBuffer = "";
        Console.SetCursorPosition(0, 0);
        winScreenBuffer += "=== GAME OVER ===\n-----------------\nRankings:\n";
        for (int i = 0; i < rankings.Count; i++)
        {
            string prefix = (i == 0) ? "👑 WINNER" : $"#{i + 1}";
            winScreenBuffer += $"{prefix}: {rankings[i].Visual} {rankings[i].Name}\n";
        }
        winScreenBuffer += "-----------------\nReturning to lobby in a few seconds...\n";
        Console.Write(winScreenBuffer);
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

    static void Render(World world, RenderSystem renderSystem, ClientPackageHandler packageHandler)
    {
        if (world.LocalCamera == null) return;
        renderSystem.Render(world, world.LocalCamera, _isChatting, _currentChatMessage,
            packageHandler.SafeMinX, packageHandler.SafeMaxX, packageHandler.SafeMinY, packageHandler.SafeMaxY);
    }
}