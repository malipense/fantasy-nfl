using Game.Core;
using Game.Infrastructure;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.WebSockets;

namespace Game.Networking
{
    internal class GameServer
    {
        private Socket socket;
        private int timeout = 120;
        private int maxPlayers = 16;
        public int ConnectionCount = 0;
        public List<FantasyPlayer> Players;
        private WebSocket webSocket;

        private static readonly TimeSpan _closeTimeout = TimeSpan.FromMilliseconds(250);
        private const int _receiveLoopBufferSize = 4 * 1024;
        private readonly int? _maxIncomingMessageSize;

        public GameServer()
        {
        }

        public void Listen()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 62000));
            socket.ReceiveTimeout = timeout;
            socket.SendTimeout = timeout;

            socket.Listen(maxPlayers);
            var handler = socket.Accept();
            while(true)
            {
                byte[] buffer = new byte[1024];
                int len = handler.Receive(buffer, SocketFlags.None);

                string reply = Handle(buffer, len, handler.RemoteEndPoint);

                handler.Send(Encoding.UTF8.GetBytes(reply));
                Console.WriteLine($"Server message: {reply}");
                ConnectionCount++;
            }
        }

        private string Handle(byte[] buffer, int len, EndPoint addr)
        {
            string reply = "";
            string m = Encoding.UTF8.GetString(buffer, 0, len);
            switch(m)
            {
                case "ELO":
                    Players.Add(new FantasyPlayer());
                    reply = $"HELO|{PlayersList.Players}";
                break;

                case "RDY":
                    reply = $"PLRDY|{addr}";
                break;
            }

            return reply;
        }
    }
}
