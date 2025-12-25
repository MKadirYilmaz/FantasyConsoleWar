namespace FantasyWar_Engine;

public class GameState
{
    public static World? GameWorld;
    public static Dictionary<int, Player> Players = new();
    
    public static void UpdatePlayer(int id, int newX, int newY)
    {
        if (Players.ContainsKey(id))
        {
            Players.TryGetValue(id, out Player? currPlayer);
            if (currPlayer == null) return;

            currPlayer.Position = new Location(newX, newY);
        }
    }
}