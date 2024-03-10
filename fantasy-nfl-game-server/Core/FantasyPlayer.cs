using System.Net;

namespace Game.Core
{
    internal class FantasyPlayer
    {
        public bool IsConnected { get; set; }
        public int ConnectionId { get; set; }
        public int PlayerId { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
    }
}
