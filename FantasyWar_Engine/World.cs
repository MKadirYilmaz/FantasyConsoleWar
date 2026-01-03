using System.Collections.Concurrent;

namespace FantasyWar_Engine;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public int[,] CollisionGrid { get; }
    public int[,] RenderGrid { get; }

    public ConcurrentDictionary<int, Entity> Entities { get; set; } = new();
    public List<string> ChatMessages { get; set; } = new();
    public bool HasAuthority { get; private set; } = false;
    
    public World(int width, int height, bool hasAuthority = false)
    {
        HasAuthority = hasAuthority;
        Width = width;
        Height = height;
        CollisionGrid = new int[width, height];
        FillGrid(CollisionGrid, -1);
        RenderGrid = new int[width, height];
        FillGrid(RenderGrid, -1);
        GenerateSimpleMap();
    }
    
    private static void FillGrid(int[,] grid, int value)
    {
        int w = grid.GetLength(0);
        int h = grid.GetLength(1);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            grid[x, y] = value;
    }
    
    public void AddOrUpdateEntity(int id, Entity entity)
    {
        if (Entities.ContainsKey(id))
        {
            Vector oldLocation = Entities[id].GetActorLocation();
            RenderGrid[oldLocation.X, oldLocation.Y] = -1;
            if(Entities[id].IsSolid)
                CollisionGrid[oldLocation.X, oldLocation.Y] = -1;
            Entities[id] = entity;
        }
        else
        {
            Entities.TryAdd(id, entity);
        }
        
        Vector location = entity.GetActorLocation();
        RenderGrid[location.X, location.Y] = id;
        if(entity.IsSolid)
            CollisionGrid[location.X, location.Y] = id;
    }
    
    public void RemoveEntity(int id)
    {
        if (Entities.TryRemove(id, out Entity? entity))
        {
            Vector location = entity.GetActorLocation();
            RenderGrid[location.X, location.Y] = -1;
            if(entity.IsSolid)
                CollisionGrid[location.X, location.Y] = -1;
        }
    }
    
    public void SetGridValue(Vector newCoordinate, Vector oldCoordinate, int entityId, bool isSolid = false)
    {
        RenderGrid[oldCoordinate.X, oldCoordinate.Y] = -1;
        if (isSolid)
        {
            CollisionGrid[oldCoordinate.X, oldCoordinate.Y] = -1;
        }
        
        RenderGrid[newCoordinate.X, newCoordinate.Y] = entityId;
        if (isSolid)
        {
            CollisionGrid[newCoordinate.X, newCoordinate.Y] = entityId;
        }
        
    }

    public Player? GetPlayer(int id)
    {
        Player? player = Entities.GetValueOrDefault(id) as Player;
        return player;
    }

    public Projectile? GetProjectile(int id)
    {
        Projectile? projectile = Entities.GetValueOrDefault(id) as Projectile;
        return projectile;
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
                CollisionGrid[x, y] = -1;

        for(int y = 0; y < Height; y++)
        {
            Entity wallLeft = EntityManager.CreateEntity(this, "Wall", "🪨", new Vector(0, y));
            Entity wallRight = EntityManager.CreateEntity(this, "Wall", "🪨", new Vector(Width - 1, y));
        }
        for(int x = 0; x < Width; x++)
        {
            Entity wallUp = EntityManager.CreateEntity(this, "Wall", "🪨", new Vector(x, 0));
            Entity wallDown = EntityManager.CreateEntity(this, "Wall", "🪨", new Vector(x, Height - 1));
        }
        
        // Add some random walls
        Random rnd = new Random(42);
        for (int i = 0; i < (int)((Height * Width) / 70) ; i++)
        {
            int rx = rnd.Next(1, Width - 1);
            int ry = rnd.Next(1, Height - 1);
            Entity wall = EntityManager.CreateEntity(this, "Wall", "🪨", new Vector(rx, ry));
        }
    }
    
    public Vector GetRandomEmptyLocation()
    {
        int x, y;
        do
        {
            x = Random.Shared.Next(1, Width - 1);
            y = Random.Shared.Next(1, Height - 1);
        } while (CollisionGrid[x, y] != -1);
        
        return new Vector(x, y);
    }
}