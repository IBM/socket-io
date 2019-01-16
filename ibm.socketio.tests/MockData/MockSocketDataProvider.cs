using System;
using System.Collections.Generic;

namespace IBM.SocketIO.Tests.MockData
{
    public class MockSocketDataProvider
    {
        #region Private Members

        private readonly List<byte> data = null;

        #endregion

        #region Ctor

        public MockSocketDataProvider(List<byte> source)
        {
            this.data = source;
        }

        #endregion

        #region Public Methods

        public (int bytesWritten, byte[] buffer) GetDataChunk(int chunkSize)
        {
            var buffer = new byte[chunkSize];
            var sentBytes = Math.Min(buffer.Length, this.data.Count);

            for (var index = 0; index < sentBytes; index++)
            {
                buffer[index] = this.data[index];
            }

            this.data.RemoveRange(0, sentBytes);

            return (bytesWritten: sentBytes, buffer: buffer);
        }

        #endregion
    }
}
