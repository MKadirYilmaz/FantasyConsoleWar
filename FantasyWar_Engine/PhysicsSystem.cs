namespace FantasyWar_Engine;

public class PhysicsSystem
{
    public void Update(World world, float deltaTime = 0.02f)
    {
        foreach (var entity in world.Entities.Values)
        {
            if (entity is Projectile projectile)
            {
                MoveProjectile(projectile, world, deltaTime);
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

    public static bool IsWalkable(Vector position)
    {
        // Map Boundary Check
        if (position.X < 0 || position.Y < 0 || position.X >= World.Instance?.Width ||
            position.Y >= World.Instance?.Height)
            return false;

        // Static Tile Collision Check
        if (World.Instance?.Grid[position.X, position.Y] != -1)
            return false;

        // Dynamic Entity Collision Check
        Entity? entity = World.Instance?.GetEntityAtPosition(position);
        if (entity != null && entity.IsSolid)
            return false;

        return true;
    }
    
    private void MoveProjectile(Projectile projectile, World world, float deltaTime)
    {
        Vector nextPos = projectile.GetActorLocation();
        
        projectile.DeltaX += projectile.Direction.X * projectile.Speed * deltaTime;
        projectile.DeltaY += projectile.Direction.Y * projectile.Speed * deltaTime;
        
        int moveX = 0;
        int moveY = 0;

        if(projectile.DeltaX >= 1f) { moveX = 1; projectile.DeltaX -= 1f; }
        else if(projectile.DeltaX <= -1f) { moveX = -1; projectile.DeltaX += 1f; }

        if(projectile.DeltaY >= 1f) { moveY = 1; projectile.DeltaY -= 1f; }
        else if(projectile.DeltaY <= -1f) { moveY = -1; projectile.DeltaY += 1f; }

        
        if (moveX == 0 && moveY == 0) return;

        nextPos.X += moveX;
        nextPos.Y += moveY;

        
        if (nextPos.X < 0 || nextPos.X >= world.Width || nextPos.Y < 0 || nextPos.Y >= world.Height ||
            world.Grid[nextPos.X, nextPos.Y] != -1)
        {
            projectile.ShouldDestroy = true;
            return;
        }

        
        Entity? hitEntity = world.GetEntityAtPosition(nextPos);
        
        if (hitEntity != null && hitEntity.IsSolid)
        {
            
            if (hitEntity.Id != projectile.OwnerId && hitEntity.Id != projectile.Id)
            {
                projectile.OnCollide(hitEntity); 
                hitEntity.OnCollide(projectile);
                projectile.ShouldDestroy = true;
            }
            else
            {
                projectile.SetActorLocation(nextPos);
            }
        }
        else
        {
            projectile.SetActorLocation(nextPos);
        }
    }
}