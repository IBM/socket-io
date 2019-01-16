using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IBM.Webclient
{
    public interface IHttpClient : IDisposable
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token);
    }
}
