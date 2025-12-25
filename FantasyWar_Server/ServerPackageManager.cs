using FantasyWar_Engine;

namespace FantasyWar_Server;

public class ServerPackageManager
{
    public static void HandlePacket(NetworkPacket? packet)
    {
        if (packet == null) return;
        
        Console.WriteLine($"Received: {packet.GetType().Name}");

        switch (packet.PacketType)
        {
            case PacketType.Login:
                LoginPacket loginPacket = (LoginPacket)packet;
                InitializePlayerInformation(loginPacket);
                break;
            case PacketType.Movement:
                MovementPacket movementPacket = (MovementPacket)packet;
                UpdatePlayerPosition(movementPacket);
                break;
            case PacketType.Chat:
                ChatPacket chatPacket = (ChatPacket)packet;
                DisplayChatMessage(chatPacket);
                break;
        }
    }
    
    private static void InitializePlayerInformation(LoginPacket loginPacket)
    {
        Console.WriteLine($"New Player Wants It's Name To Be: {loginPacket.PlayerName}");
        Player newPlayer = new Player();
        int id = GameState.Players.Count;
        newPlayer.Id = id;
        newPlayer.Name = loginPacket.PlayerName;
        
        GameState.Players.Add(newPlayer.Id, newPlayer);
        // Here you would normally send back a confirmation packet to all clients (GameState update)

    }
    
    private static void UpdatePlayerPosition(MovementPacket movementPacket)
    {
        Console.WriteLine($"Player ID: {movementPacket.PlayerId} wants to move to {movementPacket.MovementVector}");
        
        bool bCanMove = GameState.Players[movementPacket.PlayerId].AddActionPosition(movementPacket.MovementVector, GameState.GameWorld);
        if (bCanMove)
        {
            // Here you would normally broadcast the updated position to all clients (GameState update)
        }
    }
    
    private static void DisplayChatMessage(ChatPacket chatPacket)
    {
        Console.WriteLine($"Player ID: {chatPacket.PlayerId} wants to say: {chatPacket.Message}");
        
        // Here you would normally broadcast the chat message to all clients
        
    }

}