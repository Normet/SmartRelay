using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Normet.Cloud.Relay;

namespace Normet.Cloud.Relay.UnitTest
{
    [TestClass]
    public class SoveliaApiTests
    {
        SoveliaApi api;

        public SoveliaApiTests()
        {
            api = new SoveliaApi("era", "era1234");
        }

        [TestMethod]
        public void LoginTest()
        {
            var response = api.Login("era", "era1234");

            Assert.IsTrue(!string.IsNullOrEmpty(response), $"what just happened {string.IsNullOrEmpty(response)}");
        }

        [TestMethod]
        public void RevisedIBsTest()
        {
            var response = api.Login("era", "era1234");
            response = api.RevisedIBs();

            Assert.IsTrue(!string.IsNullOrEmpty(response));
        }
    }
}
