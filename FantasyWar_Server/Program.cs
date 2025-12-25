using FantasyWar_Server;
using FantasyWar_Engine;



World fantasyWorld = new World(100, 100);

Server server = new Server();
server.Start(5000, 5001);

while (true)
{
    ChatPacket packet = new ChatPacket("Server broadcast message", 0);
    server.UdpServer?.BroadcastPacket(packet);
    
    Thread.Sleep(5000);
}





