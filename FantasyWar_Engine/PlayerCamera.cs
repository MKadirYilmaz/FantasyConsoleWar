namespace FantasyWar_Engine;

public class PlayerCamera
{
    Player followedPlayer;
    int viewWidth = 25;
    int viewHeight = 10;
    
    public PlayerCamera(Player player)
    {
        followedPlayer = player;
    }
    
    public void DrawView(World world, IEnumerable<Player> players)
    {
        var (offsetX, offsetY) = GetViewOffset(world);
        
        for (int y = 0; y < viewHeight; y++)
        {
            for (int x = 0; x < viewWidth; x++)
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

        // 2\) Overlay players
        foreach (var p in players)
        {
            int sx = p.Position.X - offsetX;
            int sy = p.Position.Y - offsetY;

            if (sx < 0 || sx >= viewWidth || sy < 0 || sy >= viewHeight)
                continue;

            Console.SetCursorPosition(sx * 2, sy);
            Console.ForegroundColor = p.Color;
            Console.Write(p.Visual);
            Console.ResetColor();
        }
    }
    
    public void SetViewSize(int width, int height)
    {
        viewWidth = width;
        viewHeight = height;
    }
    
    private (int offsetX, int offsetY) GetViewOffset(World world)
    {
        int offsetX = followedPlayer.Position.X - viewWidth / 2;
        int offsetY = followedPlayer.Position.Y - viewHeight / 2;

        // Sınırları kontrol et
        if (offsetX < 0) offsetX = 0;
        if (offsetY < 0) offsetY = 0;
        if (offsetX + viewWidth > world.Width) offsetX = world.Width - viewWidth;
        if (offsetY + viewHeight > world.Height) offsetY = world.Height - viewHeight;

        return (offsetX, offsetY);
    }
    
}