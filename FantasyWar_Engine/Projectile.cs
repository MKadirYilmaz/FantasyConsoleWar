namespace FantasyWar_Engine;

public class Projectile : Entity
{
    public int OwnerId { get; set; }
    public Location Direction { get; set; }
    public int Damage { get; set; } = 10;
    public int Speed { get; set; } = 1;
    
    public Projectile(int id, int ownerId, Location direction, int speed, int damage) : base(id)
    {
        OwnerId = ownerId;
        Direction = direction;
        Speed = speed;
        Damage = damage;
        Visual = "💥";
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
}