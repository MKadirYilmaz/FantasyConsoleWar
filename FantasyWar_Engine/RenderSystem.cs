namespace FantasyWar_Engine;

public class RenderSystem
{
    string[] _firstBuffer;
    string[] _secondBuffer;
    
    bool _useFirstBuffer = true;

    public void SwapBuffers()
    {
        _useFirstBuffer = !_useFirstBuffer;
    }
    
    public void Render(World world, PlayerCamera camera)
    {
        
        var (offsetX, offsetY) = camera.GetViewOffset(world);
        int viewWidth = camera.ViewWidth;
        int viewHeight = camera.ViewHeight;
        
        DrawMapLayer(world, offsetX, offsetY, viewWidth, viewHeight);

        for (int y = 0; y < camera.ViewHeight; y++)
        {
            for (int x = 0; x < camera.ViewWidth; x++)
            {
                int worldX = x + offsetX;
                int worldY = y + offsetY;
                
                Entity? entity = world.GetEntityAtPosition(new Location(worldX, worldY));
                if (entity != null) DrawEntity(entity, offsetX, offsetY, viewWidth, viewHeight);
            }
        }
    }

    private void DrawMapLayer(World world, int offX, int offY, int w, int h)
    {
        
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Console.SetCursorPosition(x * 2, y);
                int worldX = x + offX;
                int worldY = y + offY;

                if (worldX >= 0 && worldX < world.Width && worldY >= 0 && worldY < world.Height)
                {
                    char tile = world.Grid[worldX, worldY];
                    Console.Write(tile == '#' ? "🧱" : "  ");
                }
                else
                {
                    Console.Write("  ");
                }
            }
        }
    }

    private void DrawEntity(Entity entity, int offX, int offY, int w, int h)
    {
        int screenX = entity.Position.X - offX;
        int screenY = entity.Position.Y - offY;
        
        if (screenX >= 0 && screenX < w && screenY >= 0 && screenY < h)
        {
            Console.SetCursorPosition(screenX * 2, screenY);
            Console.ForegroundColor = entity.Color;
            Console.Write(entity.Visual);
            Console.ResetColor();
        }
    }
}