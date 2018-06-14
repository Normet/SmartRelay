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
            api = new SoveliaApi("http://fi-sov-test:8080/auric/api/");
        }
    }
}
