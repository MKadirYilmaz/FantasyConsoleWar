namespace FantasyWar_Engine;

public class PlayerCamera
{ 
    private int _followedPlayerId;
    public int ViewWidth { get; private set; } = 32;
    public int ViewHeight { get; private set; } = 18;
    
    public void FollowPlayer(int playerId)
    {
        _followedPlayerId = playerId;
    }
    
    public void SetViewSize(int width, int height)
    {
        ViewWidth = width;
        ViewHeight = height;
    }
    
    public (int offsetX, int offsetY) GetViewOffset(World world)
    {
        Player? followedPlayer = world.GetPlayer(_followedPlayerId);
        if (followedPlayer == null)
        {
            return (0, 0);
        }
        int offsetX = followedPlayer.GetActorLocation().X - ViewWidth / 2;
        int offsetY = followedPlayer.GetActorLocation().Y - ViewHeight / 2;

        // Clamp offsets to world boundaries
        if (offsetX < 0) offsetX = 0;
        if (offsetY < 0) offsetY = 0;
        if (offsetX + ViewWidth > world.Width) offsetX = world.Width - ViewWidth;
        if (offsetY + ViewHeight > world.Height) offsetY = world.Height - ViewHeight;

        return (offsetX, offsetY);
    }
    
}