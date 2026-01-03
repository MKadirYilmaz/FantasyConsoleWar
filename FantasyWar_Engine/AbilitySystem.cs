namespace FantasyWar_Engine;

public class AbilitySystem
{
    private Dictionary<ProjectileType, DateTime> _lastUsedTime = new Dictionary<ProjectileType, DateTime>();
    private Dictionary<ProjectileType, int> _cooldowns = new Dictionary<ProjectileType, int>
    {
        { ProjectileType.Physical, 500 },
        { ProjectileType.Electric, 1000 },
        { ProjectileType.Fire, 2000 },
        { ProjectileType.Ice, 3000 }
    };

    public bool CanUseAbility(ProjectileType type)
    {
        if (!_lastUsedTime.ContainsKey(type))
            return true;

        return (DateTime.Now - _lastUsedTime[type]).TotalMilliseconds >= _cooldowns[type];
    }

    public void UseAbility(ProjectileType type)
    {
        _lastUsedTime[type] = DateTime.Now;
    }

    public int GetCooldown(ProjectileType type)
    {
        return _cooldowns.ContainsKey(type) ? _cooldowns[type] : 0;
    }
    
    public double GetRemainingCooldown(ProjectileType type)
    {
        if (!_lastUsedTime.ContainsKey(type))
            return 0;

        double elapsed = (DateTime.Now - _lastUsedTime[type]).TotalMilliseconds;
        double remaining = _cooldowns[type] - elapsed;
        return remaining > 0 ? remaining : 0;
    }
}

