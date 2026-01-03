using FantasyWar_Engine;

namespace FantasyWar_Client;

public class ClientGameState
{
    public int LocalPlayerId { get; set; } = -1;
    public PlayerCamera? LocalCamera { get; set; }
    public World World { get; private set; }

    public ClientGameState(World world)
    {
        World = world;
    }
}

