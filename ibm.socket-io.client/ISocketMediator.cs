using System;
using System.Threading.Tasks;
using IBM.Webclient;
using IBM.SocketIO.Factories;

namespace IBM.SocketIO
{
    public interface ISocketMediator : IDisposable
    {
        Task InitConnection(IHttpClientFactory factory, IClientSocketFactory socketFactory);
        Task Emit(string eventName, Action<string> callback);
    }
}
