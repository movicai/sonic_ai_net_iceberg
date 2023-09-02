using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CodeDom;
using System.Reflection.Emit;

namespace mus.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var fd = new mus.viewer.network.FindDevice("255.255.255.0", new Uri("/healthcheck"));
            fd.FindDeviceEvent += (s, e) =>
            {
                Console.WriteLine(e.ToString());
            };
            fd.StartFind();
        }

        public void Main(string[] args)
        {
            TestMethod1();
        }
    }
}
