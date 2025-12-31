using FantasyWar_Engine;

namespace FantasyWar_Client;

public class MainMenu
{
    private Client _client;
    private World _world;
    private ClientPackageHandler _packageHandler;
    
    private string[] _availableSkins = { "😀", "😎", "🤠", "🤡", "🤖", "👽", "👻", "👹" };
    private int _currentSkinIndex;
    private string _playerName = "Player";
    private bool _isReady;
    
    private bool _isEditingName;

    public MainMenu(Client client, World world, ClientPackageHandler packageHandler)
    {
        _client = client;
        _world = world;
        _packageHandler = packageHandler;
    }

    public void Run()
    {
        Console.Clear();
        bool inLobby = true;
        
        // Initial info update
        SendPlayerInfo();

        while (inLobby)
        {
            if (_packageHandler.GameStarted)
            {
                inLobby = false;
                break;
            }
            
            _packageHandler.ProcessPackets(_world);

            Player? localPlayer = _world.GetPlayer(_world.LocalPlayerId);
            if (localPlayer != null && localPlayer.IsWaiting)
            {
                RenderWaitingScreen();
            }
            else
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    HandleInput(key);
                }
                RenderLobby();
            }
            
            Thread.Sleep(50);
        }
    }

    private void RenderWaitingScreen()
    {
        Console.SetCursorPosition(0, 0);
        Console.WriteLine("=== FANTASY CONSOLE WAR ===".PadRight(Console.WindowWidth));
        Console.WriteLine("---------------------------".PadRight(Console.WindowWidth));
        Console.WriteLine("Game is currently in progress.".PadRight(Console.WindowWidth));
        Console.WriteLine("Please wait for the next round...".PadRight(Console.WindowWidth));
        Console.WriteLine("---------------------------".PadRight(Console.WindowWidth));
        
        for(int i=0; i<15; i++) Console.WriteLine(new string(' ', Console.WindowWidth));
    }

    private void HandleInput(ConsoleKeyInfo keyInfo)
    {
        if (_isEditingName)
        {
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                _isEditingName = false;
                SendPlayerInfo();
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (_playerName.Length > 0)
                    _playerName = _playerName.Substring(0, _playerName.Length - 1);
            }
            else if (keyInfo.KeyChar >= ' ' && _playerName.Length < 15)
            {
                _playerName += keyInfo.KeyChar;
            }
            return;
        }

        switch (keyInfo.Key)
        {
            case ConsoleKey.RightArrow:
                _currentSkinIndex = (_currentSkinIndex + 1) % _availableSkins.Length;
                SendPlayerInfo();
                break;
            case ConsoleKey.LeftArrow:
                _currentSkinIndex = (_currentSkinIndex - 1 + _availableSkins.Length) % _availableSkins.Length;
                SendPlayerInfo();
                break;
            case ConsoleKey.N:
                _isEditingName = true;
                _playerName = "";
                break;
            case ConsoleKey.R:
                _isReady = !_isReady;
                SendReadyStatus();
                break;
        }
    }

    private void SendPlayerInfo()
    {
        var packet = new UpdatePlayerInfoPacket(_world.LocalPlayerId, _playerName, _availableSkins[_currentSkinIndex]);
        _client.TcpClient.SendPacket(packet);
    }

    private void SendReadyStatus()
    {
        var packet = new PlayerReadyPacket(_world.LocalPlayerId, _isReady);
        _client.TcpClient.SendPacket(packet);
    }

    private void RenderLobby()
    {
        Console.SetCursorPosition(0, 0);
        Console.WriteLine("=== FANTASY CONSOLE WAR - LOBBY ===");
        Console.WriteLine("-----------------------------------");
        Console.WriteLine($"My Name: {_playerName} (Press 'N' to edit)");
        Console.WriteLine($"My Skin: {_availableSkins[_currentSkinIndex]} (Use Left/Right Arrows)");
        Console.WriteLine($"Status: {(_isReady ? "READY" : "NOT READY")} (Press 'R' to toggle)");
        Console.WriteLine("-----------------------------------");
        Console.WriteLine("Connected Players:");
        Console.WriteLine("");

        var players = _packageHandler.LobbyPlayers;
        
        // Draw players side by side
        string visualsLine = "";
        for (int i = 0; i < players.Count; i++)
        {
            visualsLine += $" {players[i].Visual} ";
        }
        Console.WriteLine(visualsLine.PadRight(Console.WindowWidth));
        
        string namesLine = "";
        for (int i = 0; i < players.Count; i++)
        {
            namesLine += $" {players[i].Name.Substring(0, Math.Min(players[i].Name.Length, 3))} ";
        }
        Console.WriteLine(namesLine.PadRight(Console.WindowWidth));
        
        string statusLine = "";
        for (int i = 0; i < players.Count; i++)
        {
            statusLine += $" {(players[i].IsReady ? "OK" : "..")} ";
        }
        Console.WriteLine(statusLine.PadRight(Console.WindowWidth));
        Console.WriteLine("-----------------------------------");
        
        if (players.Count < 2)
        {
            Console.WriteLine("Waiting for more players...");
        }
        else if (players.All(p => p.IsReady))
        {
            Console.WriteLine("Starting game...");
        }
        else
        {
            Console.WriteLine("Waiting for everyone to be ready...");
        }
    }
}

