namespace FantasyWar_Engine;

public class PlayerCamera
{
    Player followedPlayer;
    public int ViewWidth { get; private set; } = 25;
    public int ViewHeight { get; private set; } = 10;
    
    public void FollowPlayer(Player player)
    {
        followedPlayer = player;
    }
    
    public void DrawView(World world)
    {
        var (offsetX, offsetY) = GetViewOffset(world);
        
        for (int y = 0; y < ViewHeight; y++)
        {
            for (int x = 0; x < ViewWidth; x++)
            {
                Console.SetCursorPosition(x * 2, y);

                int worldX = x + offsetX;
                int worldY = y + offsetY;

                if (worldX >= 0 && worldX < world.Width && worldY >= 0 && worldY < world.Height)
                {
                    char tile = world.Grid[worldX, worldY];
                    if (tile == '#') Console.Write("🧱");
                    else Console.Write("  ");
                }
                else
                {
                    Console.Write("  ");
                }
            }
        }
        
        foreach (var p in world.Players.Values)
        {
            int sx = p.Position.X - offsetX;
            int sy = p.Position.Y - offsetY;

            if (sx < 0 || sx >= ViewWidth || sy < 0 || sy >= ViewHeight)
                continue;

            Console.SetCursorPosition(sx * 2, sy);
            Console.ForegroundColor = p.Color;
            Console.Write(p.Visual);
            Console.ResetColor();
        }
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