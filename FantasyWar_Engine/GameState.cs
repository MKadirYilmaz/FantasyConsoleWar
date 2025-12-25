namespace FantasyWar_Engine;

public class GameState
{
    public static Dictionary<string, Player> Players = new Dictionary<string, Player>();
    
    public static void UpdatePlayer(string id, int newX, int newY)
    {
        if (Players.ContainsKey(id))
        {
            Players[id].Position = new Location(newX, newY);
        }
    }
}