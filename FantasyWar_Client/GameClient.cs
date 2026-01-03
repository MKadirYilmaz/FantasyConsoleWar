using FantasyWar_Engine;

namespace FantasyWar_Client;

public class GameClient
{
    private Client _client;
    private ClientGameState _gameState;
    private ClientPackageHandler _packageHandler;
    private RenderSystem _renderSystem;
    
    private bool _isChatting;
    private string _currentChatMessage = "";
    private Vector _lastDirection = new Vector(0, 1);
    private ProjectileType _projectileType = ProjectileType.Physical;
    
    private const int TargetFrameRate = 60;

    public GameClient(Client client, ClientGameState gameState, ClientPackageHandler packageHandler)
    {
        _client = client;
        _gameState = gameState;
        _packageHandler = packageHandler;
        
        // Initial render system setup (will be updated when camera is ready)
        _renderSystem = new RenderSystem(50, 20);
    }

    public void Run()
    {
        while (true)
        {
            // Reset state for new game cycle
            _packageHandler.Reset();
            
            // Clear projectiles and fix ghost images
            ClearProjectilesAndGhosts();
            
            // Clear chat messages
            _gameState.World.ChatMessages.Clear();
            
            // Start Main Menu
            MainMenu mainMenu = new MainMenu(_client, _gameState, _packageHandler);
            mainMenu.Run();
            
            // Update RenderSystem with camera dimensions
            if (_gameState.LocalCamera != null)
            {
                _renderSystem = new RenderSystem(_gameState.LocalCamera.ViewWidth, _gameState.LocalCamera.ViewHeight);
            }
            
            RunGameLoop();
        }
    }

    private void RunGameLoop()
    {
        bool isRunning = true;
        while (isRunning)
        {
            if (_packageHandler.BackToLobby)
            {
                isRunning = false;
                continue;
            }

            if (_packageHandler.IsGameOver)
            {
                RenderWinScreen(_packageHandler.Rankings);
                _packageHandler.ProcessPackets(_gameState); // Keep processing to receive LobbyState/BackToLobby
                Thread.Sleep(1000);
                continue;
            }

            HandleInput();
            
            _packageHandler.ProcessPackets(_gameState);
            
            if (!_packageHandler.IsGameOver)
            {
                Render();
            }
            
            Thread.Sleep(1000 / TargetFrameRate);
        }
    }

    private void ClearProjectilesAndGhosts()
    {
        var gameWorld = _gameState.World;
        var projectiles = gameWorld.Entities.Values.OfType<Projectile>().ToList();
        foreach (var proj in projectiles)
        {
            gameWorld.RemoveEntity(proj.Id);
        }

        // Scan grid for any lingering projectile references or ghosts (desync fix)
        for (int x = 0; x < gameWorld.Width; x++)
        {
            for (int y = 0; y < gameWorld.Height; y++)
            {
                int id = gameWorld.RenderGrid[x, y];
                if (id != -1)
                {
                    if (!gameWorld.Entities.ContainsKey(id))
                    {
                        // Ghost ID in grid (entity removed but grid not cleared)
                        gameWorld.RenderGrid[x, y] = -1;
                        gameWorld.CollisionGrid[x, y] = -1;
                    }
                    else if (gameWorld.Entities[id] is Projectile)
                    {
                        // Projectile still in entities (should have been removed)
                        gameWorld.RemoveEntity(id);
                        gameWorld.RenderGrid[x, y] = -1;
                        gameWorld.CollisionGrid[x, y] = -1;
                    }
                }
            }
        }
    }

    private void HandleInput()
    {
        if (!Console.KeyAvailable) return;

        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        ConsoleKey key = keyInfo.Key;
        
        if(key == ConsoleKey.Escape)
        {
            if (_isChatting)
            {
                _isChatting = false;
                _currentChatMessage = "";
            }
        }
        else if (_isChatting)
        {
            HandleChatInput(key, keyInfo.KeyChar);
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
                HandleGameInput(key);
            }
        }
    }

    private void HandleChatInput(ConsoleKey key, char keyChar)
    {
        if (key == ConsoleKey.Enter)
        {
            if (!string.IsNullOrWhiteSpace(_currentChatMessage))
            {
                ChatPacket chatPacket = new ChatPacket(_currentChatMessage, _gameState.LocalPlayerId);
                _client.TcpClient.SendPacket(chatPacket);
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
            if (keyChar >= ' ') // Ignore control chars
            {
                _currentChatMessage += keyChar;
            }
        }
    }

    private void HandleGameInput(ConsoleKey key)
    {
        Player? localPlayer = _gameState.World.GetPlayer(_gameState.LocalPlayerId);
        if (localPlayer == null) return;

        Vector movement = new Vector(0, 0);
        switch (key)
        {
            case ConsoleKey.W:
                movement = new Vector(0, -1);
                _lastDirection = new Vector(0, -1);
                break;
            case ConsoleKey.S:
                movement = new Vector(0, 1);
                _lastDirection = new Vector(0, 1);
                break;
            case ConsoleKey.A:
                movement = new Vector(-1, 0);
                _lastDirection = new Vector(-1, 0);
                break;
            case ConsoleKey.D:
                movement = new Vector(1, 0);
                _lastDirection = new Vector(1, 0);
                break;
            case ConsoleKey.NumPad1:
                _projectileType = ProjectileType.Physical;
                break;
            case ConsoleKey.NumPad2:
                _projectileType = ProjectileType.Electric;
                break;
            case ConsoleKey.NumPad3:
                _projectileType = ProjectileType.Fire;
                break;
            case ConsoleKey.NumPad4:
                _projectileType = ProjectileType.Ice;
                break;
            case ConsoleKey.Spacebar:
                if (localPlayer.AbilitySystem.CanUseAbility(_projectileType))
                {
                    localPlayer.AbilitySystem.UseAbility(_projectileType);
                    ActionPacket actionPacket = new ActionPacket(_projectileType, _gameState.LocalPlayerId, _lastDirection);
                    _client.TcpClient.SendPacket(actionPacket);
                }
                break;
        }

        if (movement.X != 0 || movement.Y != 0)
        {
            MovementPacket movePacket = new MovementPacket(localPlayer.GetActorLocation() + movement, localPlayer.Id);
            _client.TcpClient.SendPacket(movePacket);
        }
    }

    private void Render()
    {
        if (_gameState.LocalCamera == null) return;
        _renderSystem.Render(_gameState.World, _gameState.LocalPlayerId, _gameState.LocalCamera, _isChatting, _currentChatMessage,
            _packageHandler.SafeMinX, _packageHandler.SafeMaxX, _packageHandler.SafeMinY, _packageHandler.SafeMaxY, _projectileType);
    }

    private void RenderWinScreen(List<LobbyPlayerData> rankings)
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
}

