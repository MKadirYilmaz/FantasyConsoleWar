using System.Drawing;

namespace FantasyWar_Engine;

public class RingSystem
{
    private readonly int _shrinkIntervalMs = 5000; // Shrink every 5 seconds
    private readonly int _damageIntervalMs = 1000; // Damage every 1 second
    private readonly int _damageAmount = 5;
    
    private int _mapWidth;
    private int _mapHeight;
    private bool _isActive = false;
    
    // Bounds of the SAFE zone
    public int SafeMinX { get; private set; }
    public int SafeMaxX { get; private set; }
    public int SafeMinY { get; private set; }
    public int SafeMaxY { get; private set; }

    public void Start(World world)
    {
        _mapWidth = world.Width;
        _mapHeight = world.Height;
        
        // Initially, the whole map is safe
        SafeMinX = 0;
        SafeMaxX = _mapWidth - 1;
        SafeMinY = 0;
        SafeMaxY = _mapHeight - 1;

        _isActive = true;
        
        // Start independent tasks for shrinking and damaging
        Task.Run(() => ShrinkLoop());
        Task.Run(() => DamageLoop(world));
    }

    public void Stop()
    {
        _isActive = false;
    }

    private async Task ShrinkLoop()
    {
        while (_isActive)
        {
            await Task.Delay(_shrinkIntervalMs);
            if (!_isActive) break;

            ShrinkZone();
        }
    }

    private async Task DamageLoop(World world)
    {
        while (_isActive)
        {
            await Task.Delay(_damageIntervalMs);
            if (!_isActive) break;

            ApplyRingDamage(world);
        }
    }

    private void ShrinkZone()
    {
        // Don't shrink if the zone is too small (e.g., 4x4 area left)
        if (SafeMaxX - SafeMinX < 4 || SafeMaxY - SafeMinY < 4) return;

        SafeMinX++;
        SafeMaxX--;
        SafeMinY++;
        SafeMaxY--;
        
        Console.WriteLine($"[RingSystem] Zone shrunk! Safe Area: ({SafeMinX},{SafeMinY}) to ({SafeMaxX},{SafeMaxY})");
    }

    private void ApplyRingDamage(World world)
    {
        // Iterate over a copy of values to avoid collection modification issues
        var players = world.Entities.Values.OfType<Player>().ToList();

        foreach (var player in players)
        {
            if (player.IsDead || player.IsWaiting) continue;

            Vector pos = player.GetActorLocation();
            
            // Check if player is OUTSIDE the safe zone
            if (!PhysicsSystem.CheckOverlap(pos, SafeMinX, SafeMaxX, SafeMinY, SafeMaxY))
            {
                player.TakeDamage(_damageAmount);
            }
        }
    }
}