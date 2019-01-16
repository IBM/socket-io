using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using IBM.SocketIO;

namespace Tis.AdvisoryCollector.Tests.Mocks
{
    public class MockTisSocketClient : IClientSocket
    {
        public WebSocketState ConnectionState => throw new NotImplementedException();

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string reason, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task ConnectAsync(Uri uri, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
