namespace FantasyWar_Engine;

public class EntityManager
{
    private static int _nextEntityId = 100;
    
    public static Entity CreateEntity(World world, string name, string visual, Vector position)
    {
        Entity entity = new Entity(_nextEntityId, name, position, visual);
        world.AddOrUpdateEntity(_nextEntityId, entity);
        _nextEntityId++;
        return entity;
    }
    
    public static Projectile CreateProjectile(World world, int ownerId, Vector direction, int speed, int damage, ProjectileType type)
    {
        Projectile projectile = new Projectile(_nextEntityId, ownerId, direction, speed, damage, type);
        world.AddOrUpdateEntity(_nextEntityId, projectile);
        _nextEntityId++;
        return projectile;
    }
    
    public static Player CreatePlayer(World world, string name, Vector position, string visual = "😀", ConsoleColor color = ConsoleColor.White)
    {
        Player player = new Player(_nextEntityId, name, position, visual, color);
        world.AddOrUpdateEntity(_nextEntityId, player);
        _nextEntityId++;
        return player;
    }
}