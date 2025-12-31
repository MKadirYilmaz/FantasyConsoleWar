using System.Net;
using System.Net.Sockets;
using FantasyWar_Server;
using FantasyWar_Engine;


enum GameState
{
    Lobby,
    Playing,
    GameOver
}

class Program
{
    private const int TARGET_FRAME_RATE = 60;
    private static GameState _currentState = GameState.Lobby;
    private static List<LobbyPlayerData> _initialPlayers = new List<LobbyPlayerData>();
    private static List<LobbyPlayerData> _deadPlayers = new List<LobbyPlayerData>();
    private static DateTime _gameOverTime;
    private static DateTime? _winConditionMetTime;
    private static DateTime _lastRingBroadcastTime = DateTime.MinValue;
    
    static void Main()
    {
        World serverWorld = new World(50, 50, true);
        
        PhysicsSystem physicsSystem = new PhysicsSystem();
        RingSystem ringSystem = new RingSystem();
        
        ServerPackageManager serverPackageManager = new ServerPackageManager();
        Server server = new Server();
        server.Start(5000, 5001, serverPackageManager, serverWorld);
        
        Console.WriteLine("Server started on port 5000 and UDP target port 5001.");

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Console.WriteLine($"Server IP Address: {ip}");
            }
        }
        
        while (true)
        {
            serverPackageManager.ProcessPackets(serverWorld);

            if (_currentState == GameState.Lobby)
            {
                // Broadcast Lobby State
                var lobbyPlayers = serverWorld.Entities.Values.OfType<Player>().Select(p => new LobbyPlayerData
                {
                    Id = p.Id,
                    Name = p.Name,
                    Visual = p.Visual,
                    IsReady = p.IsReady
                }).ToList();
                
                LobbyStatePacket lobbyPacket = new LobbyStatePacket(lobbyPlayers);
                server.TcpServer?.BroadcastPacket(lobbyPacket);
                
                // Check Start Condition
                if (lobbyPlayers.Count >= 2 && lobbyPlayers.All(p => p.IsReady))
                {
                    Console.WriteLine("All players ready. Starting game...");
                    _currentState = GameState.Playing;
                    if (server.TcpServer != null) server.TcpServer.IsGameRunning = true;
                    
                    ringSystem.Start(serverWorld);
                    
                    _initialPlayers = new List<LobbyPlayerData>(lobbyPlayers);
                    _deadPlayers.Clear();
                    
                    // Reset player healths and handle late joiners
                    foreach (var player in serverWorld.Entities.Values.OfType<Player>())
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
                    server.TcpServer?.BroadcastPacket(startPacket);
                }
                
                Thread.Sleep(100);
            }
            else if (_currentState == GameState.Playing)
            {
                // Broadcast Ring State periodically (every 1 second)
                if ((DateTime.Now - _lastRingBroadcastTime).TotalSeconds >= 1)
                {
                    _lastRingBroadcastTime = DateTime.Now;
                    RingStatePacket ringPacket = new RingStatePacket(
                        ringSystem.SafeMinX, ringSystem.SafeMaxX, 
                        ringSystem.SafeMinY, ringSystem.SafeMaxY
                    );
                    server.UdpServer?.BroadcastPacket(ringPacket);
                }

                while (serverPackageManager.PacketSendQueue.TryDequeue(out var packet))
                {
                    if (packet is SpawnOrDestroyPlayerPacket spawnOrDestroyPlayerPacket)
                    {
                        // Only broadcast if not waiting (though waiting players shouldn't generate these usually)
                        server.TcpServer?.BroadcastPacket(spawnOrDestroyPlayerPacket);
                    }
                    else if (packet is SpawnOrDestroyProjectilePacket spawnOrDestroyProjectilePacket)
                    {
                        server.TcpServer?.BroadcastPacket(spawnOrDestroyProjectilePacket);
                    }
                    else if (packet is ChatPacket chatPacket)
                    {
                        server.TcpServer?.BroadcastPacket(chatPacket);
                    }
                }

                var destroyedEntities = physicsSystem.Update(serverWorld, 0.02f); // Assuming 50 FPS, so deltaTime is 0.02 seconds

                foreach (var entity in destroyedEntities)
                {
                    if (entity is Projectile projectile)
                    {
                        SpawnOrDestroyProjectilePacket destroyPacket = new SpawnOrDestroyProjectilePacket(projectile, false);
                        server.TcpServer?.BroadcastPacket(destroyPacket);
                    }
                    else if (entity is Player player)
                    {
                        SpawnOrDestroyPlayerPacket destroyPacket = new SpawnOrDestroyPlayerPacket(player, false);
                        server.TcpServer?.BroadcastPacket(destroyPacket);
                        
                        // Add to dead players list
                        if (!_deadPlayers.Any(p => p.Id == player.Id) && !player.IsWaiting)
                        {
                            _deadPlayers.Add(new LobbyPlayerData { Id = player.Id, Name = player.Name, Visual = player.Visual });
                        }
                    }
                }
                
                // Check for disconnected players
                foreach (var initialPlayer in _initialPlayers)
                {
                    if (!serverWorld.Entities.ContainsKey(initialPlayer.Id) && !_deadPlayers.Any(p => p.Id == initialPlayer.Id))
                    {
                        // Player is gone from world but not in dead list -> Disconnected
                        Console.WriteLine($"Player {initialPlayer.Name} disconnected during game.");
                        _deadPlayers.Add(initialPlayer);
                    }
                }
                
                // Check Win Condition
                var alivePlayers = serverWorld.Entities.Values.OfType<Player>().Where(p => !p.IsWaiting).ToList();
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
                        // Enough time has passed, end the game
                        _currentState = GameState.GameOver;
                        ringSystem.Stop();
                        if (server.TcpServer != null) server.TcpServer.IsGameRunning = false;
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
                        server.TcpServer?.BroadcastPacket(gameOverPacket);
                        Console.WriteLine("Game Over! Winner: " + (alivePlayers.Count > 0 ? alivePlayers[0].Name : "None"));
                    }
                }
                else
                {
                    // Reset win condition timer if it's set
                    _winConditionMetTime = null;
                }
                
                foreach (Entity entity in serverWorld.Entities.Values)
                {
                    if (entity is Player player)
                    {
                        // Don't send updates for waiting players to UDP (optional, but saves bandwidth)
                        // But waiting players need to receive updates.
                        // And waiting players shouldn't be moving.
                        if (!player.IsWaiting)
                        {
                            MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                            server.UdpServer?.BroadcastPacket(moveUpdate);
                            
                            PlayerStatusPacket statusPacket = new PlayerStatusPacket(player.Id, player.Health, player.Resistance, player.CanMove, player.IsBurning);
                            server.UdpServer?.BroadcastPacket(statusPacket);
                        }
                    }
                    else if (entity is Projectile)
                    {
                        MovementPacket moveUpdate = new MovementPacket(entity.GetActorLocation(), entity.Id);
                        server.UdpServer?.BroadcastPacket(moveUpdate);
                    }
                }
                Thread.Sleep(1000 / TARGET_FRAME_RATE);
            }
            else if (_currentState == GameState.GameOver)
            {
                if ((DateTime.Now - _gameOverTime).TotalSeconds > 5)
                {
                    Console.WriteLine("Returning to Lobby...");
                    _currentState = GameState.Lobby;
                    
                    // Respawn dead players for lobby
                    foreach (var deadPlayer in _deadPlayers)
                    {
                        if (server.TcpServer != null && server.TcpServer.IsConnected(deadPlayer.Id))
                        {
                            Player newPlayer = new Player(deadPlayer.Id, deadPlayer.Name, serverWorld.GetRandomEmptyLocation(), deadPlayer.Visual);
                            serverWorld.AddOrUpdateEntity(deadPlayer.Id, newPlayer);
                            
                            // Notify clients about respawn
                            SpawnOrDestroyPlayerPacket spawnPacket = new SpawnOrDestroyPlayerPacket(newPlayer, true);
                            server.TcpServer?.BroadcastPacket(spawnPacket);
                        }
                    }
                    
                    // Also reset alive players positions and waiting players
                    foreach (var player in serverWorld.Entities.Values.OfType<Player>())
                    {
                        player.SetActorLocation(serverWorld.GetRandomEmptyLocation());
                        player.Health = player.MaxHealth;
                        player.IsReady = false;
                        
                        if (player.IsWaiting)
                        {
                            player.IsWaiting = false;
                            player.IsSolid = true;
                            player.Visual = "😀"; // Reset visual
                            
                            // Notify everyone about this player now joining properly
                            SpawnOrDestroyPlayerPacket spawnPacket = new SpawnOrDestroyPlayerPacket(player, true);
                            server.TcpServer?.BroadcastPacket(spawnPacket);
                        }
                    }
                    
                    // Clear projectiles
                    var projectiles = serverWorld.Entities.Values.OfType<Projectile>().ToList();
                    foreach (var proj in projectiles)
                    {
                        serverWorld.RemoveEntity(proj.Id);
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}












