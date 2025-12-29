using System.Collections.Concurrent;

namespace FantasyWar_Engine;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public int[,] Grid { get; }

    public static World? Instance { get; private set; }
    
    public ConcurrentDictionary<int, Player> Players = new();
    public ConcurrentDictionary<int, Entity> Entities = new();

    public bool HasAuthority { get; private set; } = false;
    
    public int LocalPlayerId { get; set; } = -1;
    public PlayerCamera? LocalCamera;
    
    public World(int width, int height, bool hasAuthority = false)
    {
        HasAuthority = hasAuthority;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            throw new InvalidOperationException("World instance already exists!");
        }
        Width = width;
        Height = height;
        Grid = new int[width, height];
        GenerateSimpleMap();
    }
    
    public void AddOrUpdateEntity(int id, Entity entity)
    {
        Vector location = entity.GetActorLocation();
        Grid[location.X, location.Y] = id;
        
        if (Entities.ContainsKey(id))
        {
            Entities[id] = entity;
        }
        else
        {
            Entities.TryAdd(id, entity);
        }
    }

    public void AddOrUpdatePlayer(int id, Player player)
    {
        if (Players.ContainsKey(id))
        {
            Players[id] = player;
        }
        else Players.TryAdd(id, player);
    }

    public Player? GetPlayer(int id)
    {
        return Players.GetValueOrDefault(id);
    }

    public Entity? GetEntityAtPosition(Vector location)
    {
        foreach(var entity in Entities.Values)
        {
            if (entity.GetActorLocation().X == location.X && entity.GetActorLocation().Y == location.Y)
                return entity;
        }

        return null;
    }

    private void GenerateSimpleMap()
    {
        // Initialize grid with empty spaces
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Grid[x, y] = -1;

        for(int y = 0; y < Height; y++)
        {
            Entity wallLeft = EntityManager.CreateEntity("Wall", "🪨", new Vector(0, y));
            Entity wallRight = EntityManager.CreateEntity("Wall", "🪨", new Vector(Width - 1, y));
            Grid[0, y] = wallLeft.Id; // Left wall
            Grid[Width - 1, y] = wallRight.Id; // Right wall
        }
        for(int x = 0; x < Width; x++)
        {
            Entity wallUp = EntityManager.CreateEntity("Wall", "🪨", new Vector(x, 0));
            Entity wallDown = EntityManager.CreateEntity("Wall", "🪨", new Vector(x, Height - 1));
            Grid[x, 0] = wallUp.Id; // Up wall
            Grid[x, Height - 1] = wallDown.Id; // Down wall
        }
        
        // Add some random walls
        Random rnd = new Random(42);
        for (int i = 0; i < (int)((Height * Width) / 70) ; i++)
        {
            int rx = rnd.Next(1, Width - 1);
            int ry = rnd.Next(1, Height - 1);
            Entity wall = EntityManager.CreateEntity("Wall", "🪨", new Vector(rx, ry));
            Grid[rx, ry] = wall.Id; 
        }
    }
    
    public Vector GetRandomEmptyLocation()
    {
        Random rnd = new Random(55);
        int x, y;
        do
        {
            x = rnd.Next(1, Width - 1);
            y = rnd.Next(1, Height - 1);
        } while (Grid[x, y] == -1);
        
        return new Vector(x, y);
    }
}