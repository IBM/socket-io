using System;
using System.Threading;
using System.Threading.Tasks;
using IBM.Webclient;
using IBM.SocketIO.Factories;
using IBM.SocketIO;

namespace Tis.AdvisoryCollector.Tests.Mocks
{
    public class MockSocketMediator : ISocketMediator
    {
        #region Private Members

        private string data = null;

        #endregion

        #region Ctor

        public MockSocketMediator(string data)
        {
            this.data = data;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the Emit() method was called
        /// </summary>
        /// <value><c>true</c> if was emit called; otherwise, <c>false</c>.</value>
        public bool WasEmitCalled { get; private set; }

        /// <summary>
        /// Causes all methods to throw exceptions when called
        /// </summary>
        /// <value><c>true</c> if throw mode; otherwise, <c>false</c>.</value>
        public bool ThrowMode { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Public Methods

        public Task Emit(string eventName, Action<string> callback)
        {
            this.WasEmitCalled = true;

            if (this.ThrowMode)
            {
                throw new Exception("Test Exception");
            }

            callback(this.data);
            return Task.CompletedTask;
        }

        public Task InitConnection(IHttpClientFactory factory, IClientSocketFactory socketFactory)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ReceiveAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
