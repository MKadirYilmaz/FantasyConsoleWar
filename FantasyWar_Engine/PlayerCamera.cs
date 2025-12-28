namespace FantasyWar_Engine;

public class PlayerCamera
{
    Player followedPlayer;
    public int ViewWidth { get; private set; } = 32;
    public int ViewHeight { get; private set; } = 18;
    
    public void FollowPlayer(Player player)
    {
        followedPlayer = player;
    }
    
    public void SetViewSize(int width, int height)
    {
        ViewWidth = width;
        ViewHeight = height;
    }
    
    public (int offsetX, int offsetY) GetViewOffset(World world)
    {
        int offsetX = followedPlayer.Position.X - ViewWidth / 2;
        int offsetY = followedPlayer.Position.Y - ViewHeight / 2;

        // Sınırları kontrol et
        if (offsetX < 0) offsetX = 0;
        if (offsetY < 0) offsetY = 0;
        if (offsetX + ViewWidth > world.Width) offsetX = world.Width - ViewWidth;
        if (offsetY + ViewHeight > world.Height) offsetY = world.Height - ViewHeight;

        return (offsetX, offsetY);
    }
    
}