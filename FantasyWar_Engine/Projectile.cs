namespace FantasyWar_Engine;

public class Projectile : Entity
{
    public int OwnerId { get; set; }
    public Vector Direction { get; set; }
    public int Damage { get; set; } = 100;
    public ProjectileType Type { get; set; } = ProjectileType.Physical;
    public int Speed { get; set; } = 2;
    
    public float DeltaX, DeltaY;
    
    public Projectile() : base()
    {
        OwnerId = -1;
        Direction = new Vector(0, 0);
        Speed = 2;
        Damage = 10;
        Visual = "💥";
        IsSolid = false;
    }
    
    public Projectile(int id, int ownerId, Vector direction, int speed, int damage, ProjectileType type) : base(id)
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
            //
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
                Speed = 5;
                Damage = 50;
                break;
            case ProjectileType.Electric:
                Visual = "⚡";
                Speed = 3;
                Damage = 25;
                break;
            case ProjectileType.Fire:
                Visual = "🔥";
                Speed = 4;
                Damage = 30;
                break;
            case ProjectileType.Ice:
                Visual = "❄️";
                Speed = 2;
                Damage = 20;
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