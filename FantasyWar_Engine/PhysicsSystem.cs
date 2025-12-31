namespace FantasyWar_Engine;

public class PhysicsSystem
{
    public List<Entity> Update(World world, float deltaTime = 0.02f)
    {
        List<Entity> destroyedEntities = new List<Entity>();
        
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
                destroyedEntities.Add(entity);
            }
        }

        foreach (var entity in destroyedEntities)
        {
            world.RemoveEntity(entity.Id);
        }

        return destroyedEntities;
    }

    public static bool IsWalkable(Vector position)
    {
        // Map Boundary Check
        if (position.X < 0 || position.Y < 0 || position.X >= World.Instance?.Width ||
            position.Y >= World.Instance?.Height)
            return false;

        // Tile Collision Check
        if (World.Instance?.CollisionGrid[position.X, position.Y] != -1)
            return false;

        return true;
    }

    public static bool CheckOverlap(Vector point, int minX, int maxX, int minY, int maxY)
    {
        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
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
            world.CollisionGrid[nextPos.X, nextPos.Y] != -1)
        {
            // Check if we hit a solid entity
            int hitEntityId = -1;
            if (nextPos.X >= 0 && nextPos.X < world.Width && nextPos.Y >= 0 && nextPos.Y < world.Height)
            {
                hitEntityId = world.CollisionGrid[nextPos.X, nextPos.Y];
            }

            Entity? hitEntity = null;
            if (hitEntityId != -1)
            {
                world.Entities.TryGetValue(hitEntityId, out hitEntity);
            }
            
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
                    // Hit owner or self -> Force move through
                    world.SetGridValue(nextPos, projectile.Position, projectile.Id, projectile.IsSolid);
                    projectile.Position = nextPos;
                }
            }
            else
            {
                // Hit map boundary or something else that is solid but not in Entities (shouldn't happen with current logic)
                projectile.ShouldDestroy = true;
            }
        }
        else
        {
            projectile.SetActorLocation(nextPos);
        }
    }
}