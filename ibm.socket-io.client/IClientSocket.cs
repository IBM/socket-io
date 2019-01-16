using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace IBM.SocketIO
{
    public interface IClientSocket : IDisposable
    {
        Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token);
        Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token);
        Task CloseAsync(WebSocketCloseStatus closeStatus, string reason, CancellationToken token);
        Task ConnectAsync(Uri uri, CancellationToken token);
        WebSocketState ConnectionState { get; }
    }
}
