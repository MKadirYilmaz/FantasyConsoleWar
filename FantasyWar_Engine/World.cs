using System.Collections.Concurrent;

namespace FantasyWar_Engine;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public char[,] Grid { get; }
    
    public ConcurrentDictionary<int, Player> Players = new();

    public int LocalPlayerId { get; set; } = -1;
    
    public World(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new char[width, height];
        GenerateSimpleMap();
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

    private void GenerateSimpleMap()
    {
        // Önce her yeri boşlukla doldur
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            Grid[x, y] = ' ';

        // Rastgele birkaç kaya ekle (Örn: # karakteri)
        Random rnd = new Random(42); // Sabit seed: Herkes aynı haritayı görsün diye!
        for (int i = 0; i < 200; i++)
        {
            int rx = rnd.Next(1, Width - 1);
            int ry = rnd.Next(1, Height - 1);
            Grid[rx, ry] = '#'; 
        }
    }

    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return Grid[x, y] != '#'; // Eğer orada kaya varsa gidemezsin
    }
}