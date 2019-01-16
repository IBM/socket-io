using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IBM.Webclient;

namespace IBM.SocketIO.Tests.Mocks
{
    public class MockHttpClient : IHttpClient
    {
        #region Private Members

        private string dataToReturn = null;
        private HttpStatusCode code = default(HttpStatusCode);
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        #endregion

        public MockHttpClient(string dataToReturn, HttpStatusCode code)
        {
            this.dataToReturn = dataToReturn;
            this.code = code;
        }

        public void Dispose()
        {
        }

        public void AddHeader(string name, string value)
        {
            this.headers.Add(name, value);
        }

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
        {
            var response = new HttpResponseMessage(this.code);

            foreach(var header in this.headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }

            response.Content = new StringContent(this.dataToReturn);

            return Task.FromResult(response);
        }
    }
}
