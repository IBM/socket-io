using System;
using IBM.SocketIO.Factories;

namespace IBM.SocketIO.Impl
{
    public class ClientSocketFactory : IClientSocketFactory
    {
        #region Public Methods

        public IClientSocket CreateSocketClient()
        {
            return new ClientSocket();
        }

        #endregion
    }
}
