using FantasyWar_Server;
using FantasyWar_Engine;

TcpGameServer server = new TcpGameServer();
server.Start(5000);

while (true)
{
    // Server main loop
    await Task.Delay(1000);
}



