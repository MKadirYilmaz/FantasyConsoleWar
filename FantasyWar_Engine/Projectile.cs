namespace FantasyWar_Engine;

public class Projectile : Entity
{
    public int OwnerId { get; set; }
    public Location Direction { get; set; }
    public int Damage { get; set; } = 100;
    public ProjectileType Type { get; set; } = ProjectileType.Physical;
    public int Speed { get; set; } = 2;
    
    public float DeltaX, DeltaY;
    
    public Projectile() : base()
    {
        OwnerId = -1;
        Direction = new Location(0, 0);
        Speed = 2;
        Damage = 10;
        Visual = "💥";
        IsSolid = false;
    }
    
    public Projectile(int id, int ownerId, Location direction, int speed, int damage, ProjectileType type) : base(id)
    {
        Id = id;
        OwnerId = ownerId;
        Direction = direction;
        Speed = speed;
        Damage = damage;
        SetType(type);
        IsSolid = false;
    }
    
    public override void OnCollide(Entity other)
    {
        if (other is Player target)
        {
            target.TakeDamage(Damage);
        }
        ShouldDestroy = true;
    }
    public void SetType(ProjectileType type)
    {
        Type = type;
        switch (type)
        {
            case ProjectileType.Physical:
                Visual = "💥";
                break;
            case ProjectileType.Electric:
                Visual = "⚡";
                break;
            case ProjectileType.Fire:
                Visual = "🔥";
                break;
            case ProjectileType.Ice:
                Visual = "❄️";
                break;
        }
    }
}

public enum ProjectileType : byte
{
    Physical = 1,
    Electric = 2,
    Fire = 3,
    Ice = 4
}