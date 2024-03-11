using System;

namespace Game.Networking
{
    internal interface IWebSocket
    {
        Action<string> OnMessage { get; set; }
        Action OnClose { get; set; }
        Action<Exception> OnError { get; set; }
        Task Send(ArraySegment<byte> message);
    }
}
