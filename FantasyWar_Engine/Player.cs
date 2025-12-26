namespace FantasyWar_Engine;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Visual { get; set; }
    
    public int Health { get; private set; } = 100;
    public int MaxHealth { get; private set; } = 100;
    public bool IsDead => Health <= 0;
    
    
    public Location Position { get; set; }
    public ConsoleColor Color { get; set; }
    
    public bool IsLocalPlayer { get; set; } = false;

    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        Visual = "😀";
        Position = new Location(1, 1);
        Color = ConsoleColor.White;
    }
    
    public void SetVisual(string visual)
    {
        Visual = visual;
    }
    
    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        Health -= damage;
        if (Health < 0) Health = 0;
    }
    
    public void Heal(int amount)
    {
        if (IsDead) return;
        
        Health += amount;
        if (Health > MaxHealth) Health = MaxHealth;
    }

    public string GetHealthBar(int length = 10)
    {
        if (MaxHealth == 0) return "";
        
        float percentage = (float)Health / MaxHealth;
        int filledLength = (int)(length * percentage);
        
        return "[" + new string('█', filledLength) + new string('░', length - filledLength) + $"] {Health}/{MaxHealth}";
    }
    

    public bool AddActorPosition(Location vector, World world)
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
    
    public bool SetActorPosition(Location newPosition, World? world = null)
    {
        if (world == null)
        {
            Position = newPosition;
            return true;
        }
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