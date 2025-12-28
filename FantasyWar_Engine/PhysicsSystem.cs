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

    public static bool IsWalkable(Location position)
    {
        // 1. Harita sınırları kontrolü
        if (position.X < 0 || position.Y < 0 || position.X >= World.Instance?.Width ||
            position.Y >= World.Instance?.Height)
            return false;

        // 2. Statik Duvar Kontrolü (Grid)
        // Grid[x,y] != -1 ise orada duvar vardır.
        if (World.Instance?.Grid[position.X, position.Y] != -1)
            return false;

        // 3. Dinamik Entity Kontrolü (Oyuncular vb.)
        // O karede Solid (katı) bir entity var mı?
        Entity? entity = World.Instance?.GetEntityAtPosition(position);
        if (entity != null && entity.IsSolid)
            return false;

        return true;
    }

    private void MoveProjectile(Projectile projectile, World world, float deltaTime)
    {
        Location nextPos = projectile.Position;
        
        // Hassas hareket hesaplaması (float biriktirme)
        projectile.DeltaX += projectile.Direction.X * projectile.Speed * deltaTime;
        projectile.DeltaY += projectile.Direction.Y * projectile.Speed * deltaTime;

        // Tam sayı koordinat değişimi var mı?
        int moveX = 0;
        int moveY = 0;

        if(projectile.DeltaX >= 1f) { moveX = 1; projectile.DeltaX -= 1f; }
        else if(projectile.DeltaX <= -1f) { moveX = -1; projectile.DeltaX += 1f; }

        if(projectile.DeltaY >= 1f) { moveY = 1; projectile.DeltaY -= 1f; }
        else if(projectile.DeltaY <= -1f) { moveY = -1; projectile.DeltaY += 1f; }

        // Eğer hareket yoksa çık
        if (moveX == 0 && moveY == 0) return;

        nextPos.X += moveX;
        nextPos.Y += moveY;

        // --- ÇARPIŞMA MANTIĞI ---

        // 1. Duvara mı çarptı? (IsWalkable false ise duvardır veya oyuncudur)
        // Ancak IsWalkable oyuncuya çarpınca da false döner, o yüzden detaylı bakmalıyız.
        
        // Sınır dışı veya Duvar (Grid) kontrolü
        if (nextPos.X < 0 || nextPos.X >= world.Width || nextPos.Y < 0 || nextPos.Y >= world.Height ||
            world.Grid[nextPos.X, nextPos.Y] != -1)
        {
            projectile.ShouldDestroy = true; // Duvara çarptı, yok et.
            return;
        }

        // 2. Bir Entity'ye (Oyuncu) mi çarptı?
        Entity? hitEntity = world.GetEntityAtPosition(nextPos);
        
        if (hitEntity != null && hitEntity.IsSolid)
        {
            // Kendini veya sahibini vurmasın
            if (hitEntity.Id != projectile.OwnerId && hitEntity.Id != projectile.Id)
            {
                projectile.OnCollide(hitEntity); // Hasar ver
                hitEntity.OnCollide(projectile);
                projectile.ShouldDestroy = true; // Çarpınca yok et
            }
            else
            {
                // Sahibinin içinden geçiyorsa pozisyonu güncelle ama yok etme
                projectile.Position = nextPos;
            }
        }
        else
        {
            // Boşluk, ilerle
            projectile.Position = nextPos;
        }
    }
}