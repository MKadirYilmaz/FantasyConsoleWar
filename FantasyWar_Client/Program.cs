using FantasyWar_Client;
using FantasyWar_Engine;

TcpGameClient gameClient = new TcpGameClient();
gameClient.Connect("127.0.0.1", 5000);

while (true)
{
    string? message = Console.ReadLine();
    if (message != null)
    {
        ChatPacket messagePacket = new ChatPacket(message, 1);
        gameClient.SendPacket(messagePacket);
    }
}