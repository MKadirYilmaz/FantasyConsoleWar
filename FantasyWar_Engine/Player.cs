namespace FantasyWar_Engine;

public class Player
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Emoji { get; set; }
    public Location Position { get; set; }
    public int Health { get; set; } = 100;
    public ConsoleColor Color { get; set; }


    public bool AddActionPosition(Location vector, World world)
    {
        Location newPosition = Position + vector;
        if (world.IsWalkable(newPosition.X, newPosition.Y))
        {
            Position = newPosition;
            return true;
        }
        else
        {
            return false;
        }
    }
    
}

public struct Location
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public Location(int x, int y)
    {
        X = x;
        Y = y;
    }
    public static Location operator +(Location a, Location b)
    {
        return new Location(a.X + b.X, a.Y + b.Y);
    }
    public static Location operator -(Location a, Location b)
    {
        return new Location(a.X - b.X, a.Y - b.Y);
    }
    public static Location operator *(Location a, int scalar)
    {
        return new Location(a.X * scalar, a.Y * scalar);
    }
    public static Location operator /(Location a, int scalar)
    {
        return new Location(a.X / scalar, a.Y / scalar);
    }
    public static bool operator ==(Location a, Location b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator !=(Location a, Location b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Location loc)
        {
            return this == loc;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}