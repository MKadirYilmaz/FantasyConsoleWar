using FantasyWar_Engine;

namespace FantasyWar_Client;

public class ClientPackageHandler
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
    
    public static void InitializePlayerInformation(LoginPacket loginPacket)
    {
        Console.WriteLine($"Player Name: {loginPacket.PlayerName}, Player ID: {loginPacket.PlayerId}");
        
        
        PlayerController.LocalPlayer.Id = loginPacket.PlayerId;
    }
    
    public static void UpdatePlayerPosition(MovementPacket movementPacket)
    {
        Console.WriteLine($"Player ID: {movementPacket.PlayerId} moved to {movementPacket.MovementVector}");
        
        if (PlayerController.LocalPlayer != null && PlayerController.LocalPlayer.Id == movementPacket.PlayerId)
        {
            PlayerController.LocalPlayer.AddActionPosition(movementPacket.MovementVector, GameState.GameWorld);
        }
    }
    
    public static void DisplayChatMessage(ChatPacket chatPacket)
    {
        if (chatPacket.PlayerId == PlayerController.LocalPlayer.Id) return;
        
        Console.WriteLine($"Player ID: {chatPacket.PlayerId} says: {chatPacket.Message}");
    }
    
    
}