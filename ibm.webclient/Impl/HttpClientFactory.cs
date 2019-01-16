using System;

namespace IBM.Webclient
{
    public class HttpClientFactory : IHttpClientFactory
    {
        private IHttpClient Client { get; set; }

        public IHttpClient CreateHttpClient()
        {
            if(this.Client == null)
            {
                this.Client = new IBMHttpClient();
            }

            return this.Client;
        }
    }
}
