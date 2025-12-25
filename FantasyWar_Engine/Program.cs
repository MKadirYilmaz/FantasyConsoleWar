using FantasyWar_Engine;

/*
Console.OutputEncoding = System.Text.Encoding.UTF8;

GameState state = new GameState();

World world = new World(100, 100);

Player me = new Player { Id = "me", Emoji = "🦖", Color = ConsoleColor.Green, Position = new Location(10, 10), Name = "PlayerOne" };

PlayerCamera camera = new PlayerCamera(me);
camera.SetViewSize(25, 10);

GameState.Players.Add(me.Id, me);

Console.CursorVisible = false;

while (true)
{
    if (Console.KeyAvailable) // Tuşa basılmış mı? (Programı durdurmaz)
    {
        var key = Console.ReadKey(true).Key;
        int nextX = me.Position.X;
        int nextY = me.Position.Y;

        if (key == ConsoleKey.W && world.IsWalkable(me.Position.X, me.Position.Y - 1))
        {
            nextY--;
        }

        if (key == ConsoleKey.S && world.IsWalkable(me.Position.X, me.Position.Y + 1))
        {
            nextY++;
        }

        if (key == ConsoleKey.A && world.IsWalkable(me.Position.X - 1, me.Position.Y))
        {
            nextX--;
        }

        if (key == ConsoleKey.D && world.IsWalkable(me.Position.X + 1, me.Position.Y))
        {
            nextX++;
        }

        // HAREKET MANTIĞI (İleride burası Server'a paket atacak)
        GameState.UpdatePlayer("me", nextX, nextY);
        
        camera.DrawView(world, GameState.Players.Values);
    }

    // İleride buraya: "Ağdan gelen verileri oku ve diğer oyuncuları güncelle" kısmı gelecek.
    Thread.Sleep(30); // CPU'yu %100 kullanmaması için kısa bir bekleme
}
*/