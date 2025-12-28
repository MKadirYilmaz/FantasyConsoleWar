using System.Text;

namespace FantasyWar_Engine;

public class RenderSystem
{
    private int _viewWidth;
    private int _viewHeight;

    public RenderSystem(int viewWidth, int viewHeight)
    {
        _viewWidth = viewWidth;
        _viewHeight = viewHeight;
        
        Console.CursorVisible = false;
    }

    public void Render(World world, PlayerCamera camera)
    {
        
        // 1. Buffer oluştur (Ekrana basılacak kareler)
        string[,] buffer = new string[_viewHeight, _viewWidth];
        ConsoleColor[,] colorBuffer = new ConsoleColor[_viewHeight, _viewWidth];

        var (offsetX, offsetY) = camera.GetViewOffset(world);

        // 2. Önce Zemin ve Statik Duvarları Çiz (Grid'den okuyarak)
        for (int y = 0; y < _viewHeight; y++)
        {
            for (int x = 0; x < _viewWidth; x++)
            {
                int worldX = offsetX + x;
                int worldY = offsetY + y;

                // Harita sınırları dışı
                if (worldX < 0 || worldX >= world.Width || worldY < 0 || worldY >= world.Height)
                {
                    buffer[y, x] = "  ";
                    continue;
                }

                // Grid kontrolü (Duvar var mı?)
                // DİKKAT: Grid[x, y] olmalı, [y, x] değil!
                int entityId = world.Grid[worldX, worldY];
                
                if (entityId != -1 && world.Entities.TryGetValue(entityId, out Entity? wall))
                {
                    buffer[y, x] = wall.Visual;
                    colorBuffer[y, x] = wall.Color;
                }
                else
                {
                    buffer[y, x] = "  "; // Zemin
                    colorBuffer[y, x] = ConsoleColor.DarkGray;
                }
            }
        }

        // 3. Dinamik Entity'leri Çiz (Oyuncular, Mermiler)
        // Grid üzerinde olmayan veya hareket halinde olan nesneler için
        foreach (var entity in world.Entities.Values)
        {
            // Entity ekranın içinde mi?
            int screenX = entity.Position.X - offsetX;
            int screenY = entity.Position.Y - offsetY;

            if (screenX >= 0 && screenX < _viewWidth && screenY >= 0 && screenY < _viewHeight)
            {
                buffer[screenY, screenX] = entity.Visual;
                colorBuffer[screenY, screenX] = entity.Color;
            }
        }

        // 4. Buffer'ı Ekrana Bas (StringBuilder ile tek seferde)
        Console.SetCursorPosition(0, 0);
        StringBuilder sb = new StringBuilder();
        
        for (int y = 0; y < _viewHeight; y++)
        {
            for (int x = 0; x < _viewWidth; x++)
            {
                // Renk desteği için burayı ileride geliştirebilirsin, şimdilik düz text
                sb.Append(buffer[y, x]);
            }
            sb.AppendLine();
        }
        Console.Write(sb.ToString());
    }
}