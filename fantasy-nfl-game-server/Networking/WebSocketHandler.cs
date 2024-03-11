using Game.Infrastructure;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;  

namespace Game.Networking
{
    internal abstract class WebSocketHandler
    {
        private static readonly TimeSpan _closeTimeout = TimeSpan.FromMilliseconds(250);
        private const int _receiveLoopBufferSize = 4 * 1024;
        private readonly int? _maxIncomingMessageSize;
        private readonly TaskQueue _sendQueue = new TaskQueue();

        protected WebSocketHandler(int? maxIncomingMessageSize) 
        { 
            _maxIncomingMessageSize = maxIncomingMessageSize;
        }

        public virtual void OnOpen() { }
        public virtual void OnMessage(string message) { throw new NotImplementedException(); }
        public virtual void OnMessage(byte[] message) { throw new NotImplementedException(); }
        public virtual void OnError() { }
        public virtual void OnClose() { }
        public virtual Task Send(string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            return SendAsync(message);
        }
        internal Task SendAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            return SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text);
        }
        internal virtual Task SendAsync(ArraySegment<byte> message, WebSocketMessageType messageType, bool endOfMessage = true) 
        { 
            if (GetWebSocketState(WebSocket) != WebSocketState.Open)
            {
                return TaskAsyncHelper.Empty;
            }

            var sendContext = new SendContext(this, message, messageType, endOfMessage);

            return _sendQueue.Enqueue(async state =>
            {
                var context = (SendContext)state;

                if (GetWebSocketState(context.Handler.WebSocket) != WebSocketState.Open)
                {
                    return;
                }

                try
                {
                    await context.Handler.WebSocket
                          .SendAsync(context.Message, context.MessageType, context.EndOfMessage, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error while sending: " + ex);
                }
            }, sendContext);
        }
        private static WebSocketState GetWebSocketState(WebSocket webSocket)
        {
            try
            {
                return webSocket.State;
            }
            catch (ObjectDisposedException) 
            {
                return WebSocketState.Closed;
            }
        }

        internal WebSocket WebSocket { get; }
    }

    internal class SendContext
    {
        public WebSocketHandler Handler;
        public ArraySegment<byte> Message;
        public WebSocketMessageType MessageType;
        public bool EndOfMessage;
        public SendContext(WebSocketHandler handler, ArraySegment<byte> message, WebSocketMessageType messageType, bool endOfMessage)
        {
            Handler = handler;
            Message = message;
            MessageType = messageType;
            EndOfMessage = endOfMessage;
        }
    }
}
