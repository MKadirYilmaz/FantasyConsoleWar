using System.Collections.Concurrent;

namespace FantasyWar_Engine;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public char[,] Grid { get; }
    
    public ConcurrentDictionary<int, Player> Players = new();
    public ConcurrentDictionary<int, Entity> Entities = new();

    public int LocalPlayerId { get; set; } = -1;
    public PlayerCamera? LocalCamera;
    
    public World(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new char[width, height];
        GenerateSimpleMap();
    }
    
    public void AddOrUpdateEntity(int id, Entity entity)
    {
        if (Entities.ContainsKey(id)) Entities[id] = entity;
        else Entities.TryAdd(id, entity);
    }

    public void AddOrUpdatePlayer(int id, Player player)
    {
        if (Players.ContainsKey(id)) Players[id] = player;
        else Players.TryAdd(id, player);
    }

    public Player? GetPlayer(int id)
    {
        if(Players.TryGetValue(id, out var player)) return player;
        return null;
    }

    public Entity? GetEntityAtPosition(Location location)
    {
        foreach(var entity in Entities.Values)
        {
            if (entity.Position.X == location.X && entity.Position.Y == location.Y)
                return entity;
        }

        return null;
    }

    private void GenerateSimpleMap()
    {
        // Önce her yeri boşlukla doldur
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            Grid[x, y] = ' ';

        for(int y = 0; y < Height; y++)
        {
            Grid[0, y] = '#'; // Sol duvar
            Grid[Width - 1, y] = '#'; // Sağ duvar
        }
        for(int x = 0; x < Width; x++)
        {
            Grid[x, 0] = '#'; // Üst duvar
            Grid[x, Height - 1] = '#'; // Alt duvar
        }
        
        // Rastgele birkaç kaya ekle (Örn: # karakteri)
        Random rnd = new Random(42); // Sabit seed: Herkes aynı haritayı görsün diye!
        for (int i = 0; i < 200; i++)
        {
            int rx = rnd.Next(1, Width - 1);
            int ry = rnd.Next(1, Height - 1);
            Grid[rx, ry] = '#'; 
        }
    }
    
    public Location GetRandomEmptyLocation()
    {
        Random rnd = new Random();
        int x, y;
        do
        {
            x = rnd.Next(1, Width - 1);
            y = rnd.Next(1, Height - 1);
        } while (Grid[x, y] == '#');
        
        return new Location(x, y);
    }

    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return Grid[x, y] != '#'; // Eğer orada kaya varsa gidemezsin
    }
}