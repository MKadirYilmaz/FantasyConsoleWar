using System.Text;

namespace FantasyWar_Engine;

public class RenderSystem
{
    private int _viewWidth;
    private int _viewHeight;
    private const int UI_HEIGHT = 4;
    private const int CHAT_HEIGHT = 5; // 1 separator + 3 messages + 1 input line

    public RenderSystem(int viewWidth, int viewHeight)
    {
        _viewWidth = viewWidth;
        _viewHeight = viewHeight;
        
        Console.CursorVisible = false;
    }

    public void Render(World world, PlayerCamera camera, bool isChatting, string currentInput,
                       int safeMinX, int safeMaxX, int safeMinY, int safeMaxY, ProjectileType selectedProjectileType)
    {
        // 1. UI Buffer (Double Width for high res text)
        int uiWidth = _viewWidth * 2;
        string[,] uiBuffer = new string[UI_HEIGHT, uiWidth];
        InitializeBuffer(uiBuffer, " ");

        Player? localPlayer = world.GetPlayer(world.LocalPlayerId);
        DrawUI(localPlayer, uiBuffer, selectedProjectileType);

        // 2. Game Buffer (Single Width, but content is double-width strings)
        string[,] gameBuffer = new string[_viewHeight, _viewWidth];
        InitializeBuffer(gameBuffer, "  ");

        var (offsetX, offsetY) = camera.GetViewOffset(world);
        
        for (int y = 0; y < _viewHeight; y++)
        {
            for (int x = 0; x < _viewWidth; x++)
            {
                int worldX = offsetX + x;
                int worldY = offsetY + y;

                if (worldX < 0 || worldX >= world.Width || worldY < 0 || worldY >= world.Height)
                {
                    gameBuffer[y, x] = "  ";
                    continue;
                }
                
                // Check Ring/Storm
                bool isInStorm = worldX < safeMinX || worldX > safeMaxX || 
                                 worldY < safeMinY || worldY > safeMaxY;

                int entityId = world.RenderGrid[worldX, worldY];
                
                if (entityId != -1 && world.Entities.TryGetValue(entityId, out Entity? entity))
                {
                    gameBuffer[y, x] = entity.Visual;
                }
                else
                {
                    if (isInStorm)
                    {
                        gameBuffer[y, x] = "▒▒"; // Visual for storm
                    }
                    else
                    {
                        gameBuffer[y, x] = "  "; 
                    }
                }
            }
        }

        // 3. Chat Buffer (Double Width for high res text)
        int chatWidth = _viewWidth * 2;
        string[,] chatBuffer = new string[CHAT_HEIGHT, chatWidth];
        InitializeBuffer(chatBuffer, " ");
        
        DrawChat(world, chatBuffer, isChatting, currentInput);

        // 4. Combine and Render
        Console.SetCursorPosition(0, 0);
        StringBuilder sb = new StringBuilder();
        
        AppendBuffer(sb, uiBuffer);
        AppendBuffer(sb, gameBuffer);
        AppendBuffer(sb, chatBuffer);
        
        Console.Write(sb.ToString());
    }

    private void InitializeBuffer(string[,] buffer, string defaultValue)
    {
        int rows = buffer.GetLength(0);
        int cols = buffer.GetLength(1);
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
                buffer[y, x] = defaultValue;
    }

    private void AppendBuffer(StringBuilder sb, string[,] buffer)
    {
        int rows = buffer.GetLength(0);
        int cols = buffer.GetLength(1);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                sb.Append(buffer[y, x]);
            }
            sb.AppendLine();
        }
    }
    
    private void DrawUI(Player? player, string[,] buffer, ProjectileType selectedProjectileType)
    {
        int width = buffer.GetLength(1);
        
        // Separator line at bottom of UI
        for (int x = 0; x < width; x++) buffer[UI_HEIGHT - 1, x] = "═";

        if (player == null)
        {
            WriteToBuffer(buffer, 0, 0, "Connecting...");
            return;
        }
        
        string healthBar = player.GetHealthBar(20); 
        string info = $"{player.Name} (ID:{player.Id}) {healthBar} {player.Health}";
        WriteToBuffer(buffer, 0, 1, info);

        // Status Effects
        string status = "Status: ";
        if (player.Resistance > 0) status += "🛡️(Resistance) ";
        if (player.Resistance < 0) status += "⚡(Shocked)/Weakened ";
        if (!player.CanMove) status += "❄️(Frozen) ";
        if (player.IsBurning) status += "🔥(Burning) ";

        WriteToBuffer(buffer, 1, 1, status);

        // Ability Cooldowns
        string abilities = "Abilities: ";
        foreach (ProjectileType type in Enum.GetValues(typeof(ProjectileType)))
        {
            string name = type.ToString();
            string selector = (type == selectedProjectileType) ? ">" : " ";
            double remaining = player.AbilitySystem.GetRemainingCooldown(type) / 1000.0;
            string cooldown = remaining > 0 ? $"({remaining:F1}s)" : "(READY)";
            
            abilities += $"{selector}{name}{cooldown} ";
        }
        WriteToBuffer(buffer, 2, 1, abilities);
    }

    private void DrawChat(World world, string[,] buffer, bool isChatting, string currentInput)
    {
        int width = buffer.GetLength(1);
        
        // Separator
        for (int x = 0; x < width; x++) buffer[0, x] = "─";
        
        // Chat messages
        int msgY = 1;
        foreach (var msg in world.ChatMessages)
        {
            if (msgY >= 4) break; // 3 messages max
            WriteToBuffer(buffer, msgY, 0, msg);
            msgY++;
        }
        
        // Input line
        if (isChatting)
        {
            WriteToBuffer(buffer, 4, 0, $"> {currentInput}_");
        }
        else
        {
            WriteToBuffer(buffer, 4, 0, "Press 'T' to chat");
        }
    }
    
    private void WriteToBuffer(string[,] buffer, int row, int col, string text)
    {
        int width = buffer.GetLength(1);
        int currentX = col;
        foreach (char c in text)
        {
            if (currentX >= width) break;
            buffer[row, currentX] = c.ToString();
            currentX++;
        }
    }
}