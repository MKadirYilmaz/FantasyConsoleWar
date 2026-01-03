using System.Collections.Concurrent;
using System.Net;
using FantasyWar_Engine;

namespace FantasyWar_Server;

public class GameLogic
{
    private const int TargetFrameRate = 60;
    private GameState _currentState = GameState.Lobby;
    private List<LobbyPlayerData> _initialPlayers = new List<LobbyPlayerData>();
    private List<LobbyPlayerData> _deadPlayers = new List<LobbyPlayerData>();
    private DateTime _gameOverTime;
    private DateTime? _winConditionMetTime;
    private DateTime _lastRingBroadcastTime = DateTime.MinValue;

    private readonly World _serverWorld;
    private readonly Server _server;
    private readonly ServerPackageManager _serverPackageManager;
    private readonly PhysicsSystem _physicsSystem;
    private readonly RingSystem _ringSystem;

    public GameLogic(World world, Server server, ServerPackageManager packageManager)
    {
        _serverWorld = world;
        _server = server;
        _serverPackageManager = packageManager;
        _physicsSystem = new PhysicsSystem();
        _ringSystem = new RingSystem();
        
        if (_server.TcpServer != null)
        {
            _server.TcpServer.OnClientConnected += HandleNewClient;
            _server.TcpServer.OnClientDisconnected += HandleClientDisconnect;
        }
    }

    private void HandleNewClient(TcpConnection clientConn)
    {
        Player spawnedPlayer = EntityManager.CreatePlayer(_serverWorld, "TestPlayer", _serverWorld.GetRandomEmptyLocation());
        
        if (_currentState == GameState.Playing)
        {
            spawnedPlayer.IsWaiting = true;
            spawnedPlayer.IsSolid = false;
            spawnedPlayer.Visual = "👻";
        }
        
        _server.TcpServer?.RegisterPlayer(spawnedPlayer.Id, clientConn);
        
        // Handle UDP Port Registration
        clientConn.OnPacketReceived += (packet) =>
        {
            if (packet is ClientUdpPortPacket udpPacket)
            {
                if (clientConn.RemoteAddress != null && _server.UdpServer != null)
                {
                    IPEndPoint endPoint = new IPEndPoint(clientConn.RemoteAddress, udpPacket.Port);
                    _server.UdpServer.AddClient(endPoint);
                    Console.WriteLine($"Registered UDP endpoint for player {udpPacket.PlayerId}: {endPoint}");
                }
            }
        };

        Console.WriteLine($"New player {spawnedPlayer.Id} has joined the game. (Waiting: {spawnedPlayer.IsWaiting})");
        
        LoginPacket loginPacket = new LoginPacket(spawnedPlayer.Name, spawnedPlayer.Id, spawnedPlayer.GetActorLocation());
        _server.TcpServer?.SendPacketTo(loginPacket, clientConn);
        
        var playersDict = new ConcurrentDictionary<int, Player>(
            _serverWorld.Entities.Values.OfType<Player>().ToDictionary(p => p.Id, p => p)
        );
        var projDict = new ConcurrentDictionary<int, Projectile>(
            _serverWorld.Entities.Values.OfType<Projectile>().ToDictionary(p => p.Id, p => p)
        );
                
        var entitiesDict = new ConcurrentDictionary<int, Entity>(
            _serverWorld.Entities.Values.Where(e => !(e is Player) && !(e is Projectile)).ToDictionary(e => e.Id, e => e)
        );
        
        // Send the current world state to the new player
        WorldPacket worldPacket = new WorldPacket(entitiesDict, playersDict, projDict);
        _server.TcpServer?.SendPacketTo(worldPacket, clientConn);
        
        if (spawnedPlayer.IsWaiting)
        {
            ChatPacket waitMsg = new ChatPacket("Game in progress. You are waiting for the next round...", -1);
            _server.TcpServer?.SendPacketTo(waitMsg, clientConn);
        }
        else
        {
            // Notify all other clients about the new player
            SpawnOrDestroyPlayerPacket spawnPacket = new SpawnOrDestroyPlayerPacket(spawnedPlayer, true);
            _server.TcpServer?.BroadcastPacket(spawnPacket);
        }
    }

    private void HandleClientDisconnect(int playerId)
    {
        Player? exitedPlayer = _serverWorld.GetPlayer(playerId);
        if(exitedPlayer == null) return;
        
        // Notify all other clients about the player leaving
        SpawnOrDestroyPlayerPacket destroyPacket = new SpawnOrDestroyPlayerPacket(exitedPlayer, false);
        _server.TcpServer?.BroadcastPacket(destroyPacket);
        
        _serverWorld.Entities.TryRemove(playerId, out _);
        
        Console.WriteLine($"Player {playerId} has disconnected and been removed from the game.");
    }

    public void Run()
    {
        while (true)
        {
            _serverPackageManager.ProcessPackets(_serverWorld);

            if (_currentState == GameState.Lobby)
            {
                UpdateLobby();
                Thread.Sleep(100);
            }
            else if (_currentState == GameState.Playing)
            {
                UpdateGame();
                Thread.Sleep(1000 / TargetFrameRate);
            }
            else if (_currentState == GameState.GameOver)
            {
                UpdateGameOver();
                Thread.Sleep(100);
            }
        }
    }

    private void UpdateLobby()
    {
        // Broadcast Lobby State
        var lobbyPlayers = _serverWorld.Entities.Values.OfType<Player>().Select(p => new LobbyPlayerData
        {
            Id = p.Id,
            Name = p.Name,
            Visual = p.Visual,
            IsReady = p.IsReady
        }).ToList();
        
        LobbyStatePacket lobbyPacket = new LobbyStatePacket(lobbyPlayers);
        _server.TcpServer?.BroadcastPacket(lobbyPacket);
        
        // Check Start Condition
        if (lobbyPlayers.Count >= 2 && lobbyPlayers.All(p => p.IsReady))
        {
            Console.WriteLine("All players ready. Starting game...");
            StartGame(lobbyPlayers);
        }
    }

    private void StartGame(List<LobbyPlayerData> lobbyPlayers)
    {
        _currentState = GameState.Playing;
        if (_server.TcpServer != null) _server.TcpServer.IsGameRunning = true;
        
        _ringSystem.Start(_serverWorld);
        
        _initialPlayers = new List<LobbyPlayerData>(lobbyPlayers);
        _deadPlayers.Clear();
        
        // Reset player healths and handle late joiners
        foreach (var player in _serverWorld.Entities.Values.OfType<Player>())
        {
            if (_initialPlayers.Any(p => p.Id == player.Id))
            {
                player.Health = player.MaxHealth;
                player.IsReady = false; // Reset ready status for next game
                player.IsWaiting = false;
            }
            else
            {
                // Player joined after lobby snapshot but before game start
                player.IsWaiting = true;
                player.IsSolid = false;
                player.Visual = "👻";
            }
        }
        
        GameStartPacket startPacket = new GameStartPacket();
        _server.TcpServer?.BroadcastPacket(startPacket);
    }

    private void UpdateGame()
    {
        // Broadcast Ring State periodically (every 1 second)
        if ((DateTime.Now - _lastRingBroadcastTime).TotalSeconds >= 1)
        {
            _lastRingBroadcastTime = DateTime.Now;
            RingStatePacket ringPacket = new RingStatePacket(
                _ringSystem.SafeMinX, _ringSystem.SafeMaxX, 
                _ringSystem.SafeMinY, _ringSystem.SafeMaxY
            );
            _server.UdpServer?.BroadcastPacket(ringPacket);
        }

        while (_serverPackageManager.PacketSendQueue.TryDequeue(out var packet))
        {
            if (packet is SpawnOrDestroyPlayerPacket spawnOrDestroyPlayerPacket)
            {
                _server.TcpServer?.BroadcastPacket(spawnOrDestroyPlayerPacket);
            }
            else if (packet is SpawnOrDestroyProjectilePacket spawnOrDestroyProjectilePacket)
            {
                _server.TcpServer?.BroadcastPacket(spawnOrDestroyProjectilePacket);
            }
            else if (packet is ChatPacket chatPacket)
            {
                _server.TcpServer?.BroadcastPacket(chatPacket);
            }
        }

        var destroyedEntities = _physicsSystem.Update(_serverWorld); // Assuming 50 FPS, so deltaTime is 0.02 seconds

        foreach (var entity in destroyedEntities)
        {
            if (entity is Projectile projectile)
            {
                SpawnOrDestroyProjectilePacket destroyPacket = new SpawnOrDestroyProjectilePacket(projectile, false);
                _server.TcpServer?.BroadcastPacket(destroyPacket);
            }
            else if (entity is Player player)
            {
                SpawnOrDestroyPlayerPacket destroyPacket = new SpawnOrDestroyPlayerPacket(player, false);
                _server.TcpServer?.BroadcastPacket(destroyPacket);
                
                // Add to dead players list
                if (!_deadPlayers.Any(p => p.Id == player.Id) && !player.IsWaiting)
                {
                    _deadPlayers.Add(new LobbyPlayerData { Id = player.Id, Name = player.Name, Visual = player.Visual });
                }
            }
        }
        
        CheckDisconnectedPlayers();
        CheckWinCondition();
        
        BroadcastWorldState();
    }

    private void CheckDisconnectedPlayers()
    {
        foreach (var initialPlayer in _initialPlayers)
        {
            if (!_serverWorld.Entities.ContainsKey(initialPlayer.Id) && !_deadPlayers.Any(p => p.Id == initialPlayer.Id))
            {
                // Player is gone from world but not in dead list -> Disconnected
                Console.WriteLine($"Player {initialPlayer.Name} disconnected during game.");
                _deadPlayers.Add(initialPlayer);
            }
        }
    }

    private void CheckWinCondition()
    {
        var alivePlayers = _serverWorld.Entities.Values.OfType<Player>().Where(p => !p.IsWaiting).ToList();
        bool shouldEndGame = false;
        
        if (_initialPlayers.Count > 1 && alivePlayers.Count <= 1)
        {
            shouldEndGame = true;
        }
        else if (_initialPlayers.Count == 1 && alivePlayers.Count == 0)
        {
            shouldEndGame = true;
        }

        if (shouldEndGame)
        {
            if (_winConditionMetTime == null)
            {
                // Win condition met for the first time
                _winConditionMetTime = DateTime.Now;
            }
            else if ((DateTime.Now - _winConditionMetTime.Value).TotalSeconds > 1)
            {
                EndGame(alivePlayers);
            }
        }
        else
        {
            // Reset win condition timer if it's set
            _winConditionMetTime = null;
        }
    }

    private void EndGame(List<Player> alivePlayers)
    {
        _currentState = GameState.GameOver;
        _ringSystem.Stop();
        if (_server.TcpServer != null) _server.TcpServer.IsGameRunning = false;
        _gameOverTime = DateTime.Now;
        
        List<LobbyPlayerData> rankings = new List<LobbyPlayerData>();
        if (alivePlayers.Count > 0)
        {
            var winner = alivePlayers[0];
            rankings.Add(new LobbyPlayerData { Id = winner.Id, Name = winner.Name, Visual = winner.Visual });
        }
        
        // Add dead players in reverse order (last dead is 2nd place)
        for (int i = _deadPlayers.Count - 1; i >= 0; i--)
        {
            // Avoid adding winner if they are somehow in dead list (shouldn't happen)
            if (alivePlayers.Count > 0 && _deadPlayers[i].Id == alivePlayers[0].Id) continue;
            
            rankings.Add(_deadPlayers[i]);
        }
        
        GameOverPacket gameOverPacket = new GameOverPacket(rankings);
        _server.TcpServer?.BroadcastPacket(gameOverPacket);
        Console.WriteLine("Game Over! Winner: " + (alivePlayers.Count > 0 ? alivePlayers[0].Name : "None"));
    }

    private void BroadcastWorldState()
    {
        foreach (Entity entity in _serverWorld.Entities.Values)
        {
            if (entity is Player player)
            {
                if (!player.IsWaiting)
                {
                    MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                    _server.UdpServer?.BroadcastPacket(moveUpdate);
                    
                    PlayerStatusPacket statusPacket = new PlayerStatusPacket(player.Id, player.Health, player.Resistance, player.CanMove, player.IsBurning);
                    _server.UdpServer?.BroadcastPacket(statusPacket);
                }
            }
            else if (entity is Projectile)
            {
                MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                _server.UdpServer?.BroadcastPacket(moveUpdate);
            }
        }
    }

    private void UpdateGameOver()
    {
        if ((DateTime.Now - _gameOverTime).TotalSeconds > 5)
        {
            Console.WriteLine("Returning to Lobby...");
            _currentState = GameState.Lobby;
            
            // Respawn dead players for lobby
            foreach (var deadPlayer in _deadPlayers)
            {
                if (_server.TcpServer != null && _server.TcpServer.IsConnected(deadPlayer.Id))
                {
                    Player newPlayer = new Player(deadPlayer.Id, deadPlayer.Name, _serverWorld.GetRandomEmptyLocation(), deadPlayer.Visual);
                    _serverWorld.AddOrUpdateEntity(deadPlayer.Id, newPlayer);
                    
                    // Notify clients about respawn
                    SpawnOrDestroyPlayerPacket spawnPacket = new SpawnOrDestroyPlayerPacket(newPlayer, true);
                    _server.TcpServer?.BroadcastPacket(spawnPacket);
                }
            }
            
            // Also reset alive players positions and waiting players
            foreach (var player in _serverWorld.Entities.Values.OfType<Player>())
            {
                player.SetActorLocation(_serverWorld.GetRandomEmptyLocation(), _serverWorld);
                player.Health = player.MaxHealth;
                player.IsReady = false;
                
                if (player.IsWaiting)
                {
                    player.IsWaiting = false;
                    player.IsSolid = true;
                    player.Visual = "😀"; // Reset visual
                    
                    // Notify everyone about this player now joining properly
                    SpawnOrDestroyPlayerPacket spawnPacket = new SpawnOrDestroyPlayerPacket(player, true);
                    _server.TcpServer?.BroadcastPacket(spawnPacket);
                }
            }
            
            // Clear projectiles
            var projectiles = _serverWorld.Entities.Values.OfType<Projectile>().ToList();
            foreach (var proj in projectiles)
            {
                _serverWorld.RemoveEntity(proj.Id);
            }
        }
    }
}

public enum GameState
{
    Lobby,
    Playing,
    GameOver
}

