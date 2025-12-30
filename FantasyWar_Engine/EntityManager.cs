namespace FantasyWar_Engine;

public class EntityManager
{
    private static int _nextEntityId = 100;
    
    public static Entity CreateEntity(string name, string visual, Vector position)
    {
        Entity entity = new Entity(_nextEntityId, name, position, visual);
        World.Instance?.AddOrUpdateEntity(_nextEntityId, entity);
        _nextEntityId++;
        return entity;
    }
    
    public static Projectile CreateProjectile(int ownerId, Vector direction, int speed, int damage, ProjectileType type)
    {
        Projectile projectile = new Projectile(_nextEntityId, ownerId, direction, speed, damage, type);
        World.Instance?.AddOrUpdateEntity(_nextEntityId, projectile);
        _nextEntityId++;
        return projectile;
    }
    
    public static Player CreatePlayer(string name, Vector position, string visual = "😀", ConsoleColor color = ConsoleColor.White)
    {
        Player player = new Player(_nextEntityId, name, position, visual, color);
        World.Instance?.AddOrUpdateEntity(_nextEntityId, player);
        _nextEntityId++;
        return player;
    }
}