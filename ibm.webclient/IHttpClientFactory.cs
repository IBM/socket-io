using System;

namespace IBM.Webclient
{
    public interface IHttpClientFactory
    {
        IHttpClient CreateHttpClient();
    }
}
