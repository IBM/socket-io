using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace IBM.SocketIO.Impl
{
    public class ClientSocket : IClientSocket
    {
        #region Private Members

        private ClientWebSocket socketClient = null;

        #endregion

        #region Ctor

        public ClientSocket()
        {
            this.socketClient = new ClientWebSocket();
            this.socketClient.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        }

        #endregion

        #region Public Properties

        public WebSocketState ConnectionState
        {
            get
            {
                return this.socketClient.State;
            }
        }

        #endregion

        #region Public Methods

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string reason, CancellationToken token)
        {
            return this.socketClient.CloseAsync(closeStatus, reason, token);
        }

        public Task ConnectAsync(Uri uri, CancellationToken token)
        {
            return this.socketClient.ConnectAsync(uri, token);
        }

        public void Dispose()
        {
            this.socketClient.Dispose();
        }

        public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            return await this.socketClient.ReceiveAsync(buffer, token);
        }

        public async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token)
        {
            await this.socketClient.SendAsync(buffer, messageType, endOfMessage, token);
        }

        #endregion
    }
}
