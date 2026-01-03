using FantasyWar_Engine;

namespace FantasyWar_Client;

public class PlayerCamera
{
    public int ViewWidth { get; set; } = 50;
    public int ViewHeight { get; set; } = 20;
    
    private int _targetPlayerId = -1;

    public void FollowPlayer(int playerId)
    {
        _targetPlayerId = playerId;
    }

    public (int offsetX, int offsetY) GetViewOffset(World world)
    {
        if (_targetPlayerId == -1) return (0, 0);

        Player? target = world.GetPlayer(_targetPlayerId);
        if (target == null) return (0, 0);

        Vector pos = target.GetActorLocation();
        
        int offsetX = pos.X - (ViewWidth / 2);
        int offsetY = pos.Y - (ViewHeight / 2);
        
        // Clamp to world bounds
        if (offsetX < 0) offsetX = 0;
        if (offsetY < 0) offsetY = 0;
        if (offsetX > world.Width - ViewWidth) offsetX = world.Width - ViewWidth;
        if (offsetY > world.Height - ViewHeight) offsetY = world.Height - ViewHeight;

        return (offsetX, offsetY);
    }
}

