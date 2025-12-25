using FantasyWar_Client;
using FantasyWar_Engine;

Client client = new Client("127.0.0.1", 5000, 5001);

while (true)
{
    string? message = Console.ReadLine();
    if (message != null)
    {
        ChatPacket messagePacket = new ChatPacket(message, 1);
        client.TcpClient.SendPacket(messagePacket);
    }
}