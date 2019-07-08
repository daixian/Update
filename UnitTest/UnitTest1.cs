using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flurl;
using Flurl.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            Task.Run(async () =>
            {
                var msg = await "http://mr.xuexuesoft.com:8012/hi".GetAsync();
                string str = msg.ToString();
                semaphore.Release();
            });

            semaphore.Wait();
        }
    }
}
