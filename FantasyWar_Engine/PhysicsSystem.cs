namespace FantasyWar_Engine;

public class PhysicsSystem
{
    public void Update(World world)
    {
        foreach (var entity in world.Entities.Values)
        {
            if (entity is Projectile projectile)
            {
                MoveProjectile(projectile, world);
            }
        }
        
        foreach (var entity in world.Entities.Values)
        {
            if (entity.ShouldDestroy)
            {
                world.Entities.TryRemove(entity.Id, out _);
            }
        }
    }

    private void MoveProjectile(Projectile projectile, World world)
    {
        Location nextPos = projectile.Position + projectile.Direction;

        if (!world.IsWalkable(nextPos.X, nextPos.Y))
        {
            projectile.ShouldDestroy = true;
            return;
        }
        
        Entity? hitEntity = world.GetEntityAtPosition(nextPos);
        if (hitEntity != null && hitEntity.IsSolid && hitEntity.Id != projectile.OwnerId && hitEntity.Id != projectile.Id)
        {
            projectile.OnCollide(hitEntity);
            hitEntity.OnCollide(projectile);
        }
        else
        {
            projectile.Position = nextPos;
        }
    }
}