// See https://aka.ms/new-console-template for more information
using Game.Networking;

Console.WriteLine("Hello, World!");

GameServer server = new GameServer();
server.Listen();

if(server.ConnectionCount > 16)
{
    Console.WriteLine("League is full");
}

