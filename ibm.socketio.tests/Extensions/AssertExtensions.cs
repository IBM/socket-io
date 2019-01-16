using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IBM.SocketIO.Tests.Extensions
{
    public static class AsyncAssert
    {
        #region Public Static Methods

        public static async Task Throws<T>(Func<Task> func) where T: Exception
        {
            try
            {
                await func();
            }
            catch(Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
            }
        }

        public static async Task Throws<T, TOut>(Func<Task<TOut>> func) where T: Exception
        {
            try
            {
                await func();
            }
            catch(Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
            }
        }

        #endregion
    }
}
