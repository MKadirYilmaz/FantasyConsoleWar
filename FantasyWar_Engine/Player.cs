namespace FantasyWar_Engine;

public class Player : Entity
{
    public int Health { get; set; } = 100;
    public int MaxHealth = 100;
    public bool IsDead => Health <= 0;

    public event Action<ProjectileType>? OnHit;

    public int Resistance { get; set; } = 0;
    public bool CanMove { get; set; } = true;
    public bool IsBurning { get; set; } = false;
    
    private DateTime _burnEndTime;
    
    
    public bool IsLocalPlayer { get; set; } = false;
    public Player() : base()
    {
        Id = -1;
        Name = "Player";
        Visual = "😀";
        Position = new Location(1, 1);
        Color = ConsoleColor.White;

        IsSolid = true;
    }
    
    public Player(int id) : base(id)
    {
        Id = id;
        Name = "Player";
        Visual = "😀";
        Position = new Location(1, 1);
        Color = ConsoleColor.White;

        IsSolid = true;
    }
    public Player(int id, string name, Location position, string visual = "😀", ConsoleColor color = ConsoleColor.White) : base(id)
    {
        Id = id;
        Name = name;
        Position = position;
        Visual = visual;
        Color = color;
        
        IsSolid = true;
    }
    
    public void SetVisual(string visual)
    {
        Visual = visual;
    }
    
    public void TakeDamage(int damage)
    {
        float resistanceFactor = 100 - Resistance; // 0 - 200
        Health -= (int)(damage * (resistanceFactor / 100));
        
        if (Health <= 0)
        {
            World.Instance?.Players.TryRemove(Id, out _);
            World.Instance?.Entities.TryRemove(Id, out _);
        }
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
        
        int currentHealth = Health < 0 ? 0 : Health;
        
        float percentage = (float)currentHealth / MaxHealth;
        int filledLength = (int)(length * percentage);
        
        return "[" + new string('█', filledLength) + new string('░', length - filledLength) + $"] {Health}/{MaxHealth}";
    }
    

    public bool AddActorPosition(Location vector, World? world = null)
    {
        Location newPosition = Position + vector;
        if (world == null)
        {
            Position = newPosition;
            return true;
        }
        if (PhysicsSystem.IsWalkable(newPosition))
        {
            Position = newPosition;
            return true;
        }

        return false;
    }
    
    public bool SetActorPosition(Location newPosition, World? world = null)
    {
        if (world == null)
        {
            Position = newPosition;
            return true;
        }
        if (PhysicsSystem.IsWalkable(newPosition))
        {
            Position = newPosition;
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public override void OnCollide(Entity other)
    {
        if (other is Projectile projectile)
        {
            switch (projectile.Type)
            {
                case ProjectileType.Physical:
                    TakeDamage(projectile.Damage);
                    break;
                case ProjectileType.Electric:
                    Task.Run(() => ShockPlayer(projectile));
                    break;
                case ProjectileType.Fire:
                    Task.Run(() => BurnPlayer(projectile));
                    break;
                case ProjectileType.Ice:
                    Task.Run(() => FreezePlayer(projectile));
                    break;
            }
        }
    }
    
    private async Task BurnPlayer(Projectile projectile)
    {
        IsBurning = true;
        _burnEndTime = DateTime.Now.AddSeconds(5);
    
        int burnDamage = projectile.Damage / 5;
        for (int i = 0; i < 5; i++)
        {
            if (IsDead) break;
            TakeDamage(burnDamage);
            await Task.Delay(1000);
        }
        IsBurning = false;
    }
    
    private async Task ShockPlayer(Projectile projectile)
    {
        TakeDamage(projectile.Damage);

        Resistance = -50;
        await Task.Delay(5000);
        Resistance = 0;
    }
    
    private async Task FreezePlayer(Projectile projectile)
    {
        TakeDamage(projectile.Damage);

        Resistance = 80;
        CanMove = false;
        await Task.Delay(3000);
        CanMove = true;
        Resistance = 0;
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