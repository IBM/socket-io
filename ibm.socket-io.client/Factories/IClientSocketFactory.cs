using System;
namespace IBM.SocketIO.Factories
{
    public interface IClientSocketFactory
    {
        IClientSocket CreateSocketClient();
    }
}
