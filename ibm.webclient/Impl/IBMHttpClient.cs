using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IBM.Webclient
{
    public class IBMHttpClient : IHttpClient, IDisposable
    {
        #region Private Members

        private HttpClient client = null;

        #endregion

        #region Ctor

        public IBMHttpClient()
        {
            this.client = new HttpClient();
        }

        #endregion

        #region Public Methods

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
        {
            return this.client.SendAsync(message, token);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        #endregion
    }
}
