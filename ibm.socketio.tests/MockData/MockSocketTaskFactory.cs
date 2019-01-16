using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace IBM.SocketIO.Tests.MockData
{
    public class MockSocketTaskFactory
    {
        #region Public Static Methods

        public static Task<WebSocketReceiveResult> CreateTask(int bytesLeft = 0, int bytesWritten = 0)
        {
            return Task.FromResult(bytesLeft > 0 ?
                        new WebSocketReceiveResult(
                            bytesWritten, WebSocketMessageType.Binary, false)
                        : new WebSocketReceiveResult(
                            bytesWritten, WebSocketMessageType.Binary, true));
        }

        #endregion
    }
}
