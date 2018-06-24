using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Normet.Cloud.Relay;
using Newtonsoft.Json.Linq;

namespace Normet.Cloud.Relay.UnitTest
{
    [TestClass]
    public class SoveliaApiTests
    {
        JObject queryAsset;

        public SoveliaApiTests()
        {
            //api = new SoveliaApi("http://fi-sov-test:8080/auric/api/");
        }

        [TestInitialize]
        public void initialize()
        {
            queryAsset = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "Typetree", value = new []{ "Installed base management,Equipment" } },
                    new { name = "HasParents", value = new [] { "false" } },
                    new { name = "HasChildren", value = new [] { "true" } },
                    new { name = "StatLatestRel", value = new [] { "true" } },
                    new { name = "LastRev", value = new [] { "true" } },
                    new { name = "RevDate", value = new [] { $"[-14d]-*" } }
                }
            });
        }


        [TestMethod]
        public void LoginTest()
        {
            var auth = SoveliaApi.Login("era", "era1234", "fi-sov-test:8080");

            Assert.IsTrue(auth.Contains("token"));
        }

        [TestMethod]
        public void SearchAssetTest()
        {
            var auth = SoveliaApi.Login("era", "era1234", "fi-sov-test:8080");

            var result = SoveliaApi.Search(auth, queryAsset.ToString(), "minbasket=6&superbasket=CUSTOMER,BASEITEMCODE,BASEITEMREVISION,DocID,MODEL,CUSTOMERSERIALNUMBER,SERIALNUMBER,REALENGINEHOURS,CURRENTCUSTOMER,MINEORTUNNELSITE,STARTUPDATE");

            Assert.IsNotNull(result);
        }
    }
}
