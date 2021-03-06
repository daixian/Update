﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flurl;
using Flurl.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRegex()
        {
            string text = "server:AuxiliaryService\r\n";
            Match m = Regex.Match(text, @"^server\s*:\s*(\S+)\s*");//\s为空白符,\S为非空白符
            Assert.IsTrue(m.Success);
            string result = m.Groups[1].Value;
            Assert.AreEqual(result, "AuxiliaryService");

        }

        [TestMethod]
        public void TestRegex2()
        {
            string text = "server:AuxiliaryService";
            Match m = Regex.Match(text, @"^server\s*:\s*(\S+)\s*");
            Assert.IsTrue(m.Success);

            string result = m.Groups[1].Value;
            Assert.AreEqual(result, "AuxiliaryService");

        }


        [TestMethod]
        public void TestGetAsync1()
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

        [TestMethod]
        public void TestGetAsync2()
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            Task.Run(async () =>
            {
                //首先必须要用try包围,因为如果连接有问题那么会抛出一个异常
                try
                {
                    //如果是404按照常规情况也会抛出,如果使用AllowAnyHttpStatus()则可以接受404的状态不会异常
                    var msg = await "https://home.xuexuesoft.com:8010/update/".AllowAnyHttpStatus().GetAsync();
                    string str = msg.ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                semaphore.Release();
            });

            semaphore.Wait();
        }


        [TestMethod]
        public void TestGetStringAsync()
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            Task.Run(async () =>
            {
                try
                {
                    string rtext = await "http://mr.xuexuesoft.com:8012/hi".GetStringAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                semaphore.Release();
            });
            semaphore.Wait();
        }
    }
}
