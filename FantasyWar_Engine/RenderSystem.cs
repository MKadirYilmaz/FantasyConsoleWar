using System.Text;

namespace FantasyWar_Engine;

public class RenderSystem
{
    private int _viewWidth;
    private int _viewHeight;
    private const int UI_HEIGHT = 3;

    public RenderSystem(int viewWidth, int viewHeight)
    {
        _viewWidth = viewWidth;
        _viewHeight = viewHeight;
        
        Console.CursorVisible = false;
    }

    public void Render(World world, PlayerCamera camera)
    {
        int totalHeight = _viewHeight + UI_HEIGHT;
        
        // Create buffer
        string[,] buffer = new string[totalHeight, _viewWidth];
        ConsoleColor[,] colorBuffer = new ConsoleColor[totalHeight, _viewWidth];

        Player? localPlayer = world.GetPlayer(world.LocalPlayerId);
        DrawUI(localPlayer, buffer, colorBuffer);
        
        var (offsetX, offsetY) = camera.GetViewOffset(world);
        
        // Draw static world (walls, ground)
        for (int y = 0; y < _viewHeight; y++)
        {
            for (int x = 0; x < _viewWidth; x++)
            {
                int bufferY = y + UI_HEIGHT;
                
                int worldX = offsetX + x;
                int worldY = offsetY + y;

                // Out of bounds check
                if (worldX < 0 || worldX >= world.Width || worldY < 0 || worldY >= world.Height)
                {
                    buffer[bufferY, x] = "  ";
                    continue;
                }
                
                int entityId = world.RenderGrid[worldX, worldY];
                
                if (entityId != -1 && world.Entities.TryGetValue(entityId, out Entity? wall))
                {
                    buffer[bufferY, x] = wall.Visual;
                    colorBuffer[bufferY, x] = wall.Color;
                }
                else
                {
                    buffer[bufferY, x] = "  "; // Empty ground
                    colorBuffer[bufferY, x] = ConsoleColor.DarkGreen;
                }
            }
        }
        

        // Render buffer to console
        Console.SetCursorPosition(0, 0);
        StringBuilder sb = new StringBuilder();
        
        for (int y = 0; y < totalHeight; y++)
        {
            for (int x = 0; x < _viewWidth; x++)
            {
                sb.Append(buffer[y, x]);
            }
            sb.AppendLine();
        }
        Console.Write(sb.ToString());
    }
    
    private void DrawUI(Player? player, string[,] buffer, ConsoleColor[,] colorBuffer)
    {
        // Clear UI area
        for (int y = 0; y < UI_HEIGHT; y++)
        for (int x = 0; x < _viewWidth; x++)
            buffer[y, x] = " "; 

        // Çerçeve veya ayırıcı çizgi (Opsiyonel)
        for (int x = 0; x < _viewWidth; x++) buffer[UI_HEIGHT - 1, x] = "═";

        if (player == null)
        {
            WriteToBuffer(buffer, 0, 0, "Connecting...");
            return;
        }
        
        string healthBar = player.GetHealthBar(10); // [████░░] 80/100
        string info = $"{player.Name} (ID:{player.Id}) {healthBar} {player.Health}";
        WriteToBuffer(buffer, 0, 1, info);

        // Status Effects
        string status = "Status: ";
        if (player.Resistance > 0) status += "🛡️(Resistance) ";
        if (player.Resistance < 0) status += "⚡(Shocked)/Weakened ";
        if (!player.CanMove) status += "❄️(Frozen) ";
        if (player.IsBurning) status += "🔥(Burning) ";

        WriteToBuffer(buffer, 1, 1, status);
    }
    
    private void WriteToBuffer(string[,] buffer, int row, int col, string text)
    {
        int currentX = col;
        foreach (char c in text)
        {
            if (currentX >= _viewWidth) break;
            buffer[row, currentX] = c.ToString();
            currentX++;
        }
    }
}