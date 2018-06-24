using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Normet.Cloud.Relay
{
    public class SoveliaApi
    {
        private static string EQUIPMENT = "Installed base management,Equipment";
        private static string SERVICEMODULE = "Installed base management,Services,Modules";
        private static string SERVICEITEM = "Installed base management,Services,Tasks";

        private static string GetServiceUri(
            string hostname
            )
        {
            return $"http://{hostname}/auric/api/rest";
        }

        #region Login
        /// <summary>
        /// Login to the sovelia and returns the authentication object
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="hostname">Sovelia hostname with port ServerName:Port</param>
        /// <returns></returns>
        public static string Login(
            string username,
            string password,
            string hostname)
        {
            var client = new RestClient($"{GetServiceUri(hostname)}/user/login");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("undefined", string.Format($"username={username}&password={password}&longsession=true"), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            var auth = JObject.Parse(response.Content);
            if (auth.ContainsKey("httpCode"))
            {
                var httpCode = (string)auth["httpCode"];
                switch ($"{auth["httpCode"]}")
                {
                    case "200":
                        auth.Add("token", $"{response.Headers.FirstOrDefault(i => i.Name == "X-Access-Token").Value}");
                        auth.Add("dateTime", DateTime.Now);
                        auth.Add("cookies", JArray.FromObject(response.Cookies));
                        auth.Add("baseuri", $"{GetServiceUri(hostname)}");
                        break;
                }
            }
            return auth.ToString();
        }
        #endregion

        #region Search
        public static string Search(
            string auth,
            string criterias,
            string basket = "minbasket=1")
        {
            return Search(JObject.Parse(auth), criterias, basket);
        }
        public static string Search(
            JObject auth, 
            string criterias, 
            string basket = "minbasket=1")
        {
            var client = new RestClient($"{auth["baseuri"]}/search?{basket}");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("X-Access-Token", $"{auth["token"]}");
            request.AddParameter("undefined", $"{criterias}", ParameterType.RequestBody);
            foreach(var cookie in auth["cookies"])
            {
                request.AddCookie($"{cookie["Name"]}", $"{cookie["Value"]}");
            }
            IRestResponse response = client.Execute(request);
            JObject searchResult = JObject.Parse(response.Content);
            return searchResult.ToString();
        }
        #endregion

        public static List<CustomerAsset> GetAllAssets(
            string auth,
            string revDate = "[-7d]-*",
            bool cascade = false)
        {
            List<CustomerAsset> customerAssetList = new List<CustomerAsset>();
            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "Typetree", value = new []{ EQUIPMENT } },
                    new { name = "HasParents", value = new [] { "false" } },
                    new { name = "HasChildren", value = new [] { "true" } },
                    new { name = "StatLatestRel", value = new [] { "true" } },
                    new { name = "LastRev", value = new [] { "true" } },
                    new { name = "RevDate", value = new [] { $"{revDate}" } },
                    new {name="StatusDescription", value=new[] {"Checked"}}
                }
            });

            var searchResult = Search(auth, query.ToString(), "minbasket=1&superbasket=CUSTOMER,BASEITEMCODE,BASEITEMREVISION,MODEL,CUSTOMERSERIALNUMBER,SERIALNUMBER,REALENGINEHOURS,CURRENTCUSTOMER,MINEORTUNNELSITE,STARTUPDATE,VEHICLEUSAGESTATUS");

            var ibs = JObject.Parse(searchResult);
            if (ibs["result"]["dto"].Count() > 0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var customerAsset = new CustomerAsset()
                    {
                        Customer = ib.GetStringValue("CUSTOMER"),
                        ItemCode = ib.GetStringValue("BASEITEMCODE"),
                        ItemRev = ib.GetStringValue("BASEITEMREVISION"),
                        DocId = ib.GetStringValue("DocID"),
                        Model = ib.GetStringValue("MODEL"),
                        CustomerSerialNumber = ib.GetStringValue("CUSTOMERSERIALNUMBER"),
                        SerialNumber = ib.GetStringValue("SERIALNUMBER"),
                        RealEngineHours = ib.GetStringValue("REALENGINEHOURS"),
                        CurrentCustomer = ib.GetStringValue("CURRENTCUSTOMER"),
                        Site = ib.GetStringValue("MINEORTUNNELSITE"),
                        StartupDate = ib.GetStringValue("STARTUPDATE"),
                        VehicleUsageStatus = ib.GetStringValue("VEHICLEUSAGESTATUS")
                    };
                    if (cascade)
                    {
                        customerAsset.Modules = GetAssetModules(auth, customerAsset.DocId);
                    }
                    customerAssetList.Add(customerAsset);
                }
            }
            return customerAssetList;
        }

        public static List<AssetModule> GetAssetModules(
            string auth,
            string id)
        {
            var assetModules = new List<AssetModule>();
            var asset = new AssetModule()
            {
                ItemNumber = id,
                Description = "General maintenance service module"
            };
            asset.AddServiceTaskRange(GetModuleServices(auth, id));
            assetModules.Add(asset);

            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{id}" } },
                    new { name = "Typetree", value = new [] { SERVICEMODULE } }
                }
            });

            var searchResult = Search(auth, query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr");
            var ibs = JObject.Parse(searchResult);
            if (ibs["result"]["dto"].Count() > 0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var x = ib["baskets"][0];
                    var docId = x.GetStringValue("DocID");
                    asset = new AssetModule()
                    {
                        ItemNumber = $"{docId}",
                        Description = x.GetStringValue("DocDescr")
                    };
                    asset.AddServiceTaskRange(GetSubModuleServices(auth, docId));
                    assetModules.Add(asset);
                }
            }

            return assetModules;
        }

        public static List<ServiceTask> GetSubModuleServices(
            string auth,
            string id)
        {
            var serviceTasks = new List<ServiceTask>();

            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{id}" } },
                    new { name = "HasChildren", value = new [] { $"true" } },
                    new { name = "Typetree", value = new [] { "Installed base management,Components,Other components" } },

                }
            });

            var searchResult = Search(auth, query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr,HasChildren");
            var ibs = JObject.Parse(searchResult);
            if (ibs["result"]["dto"].Count() > 0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var x = ib["baskets"][0];
                    string docid = x.GetStringValue("DocID");                    

                    serviceTasks.AddRange(GetModuleServices(auth, docid));
                }
            }

            return serviceTasks;
        }

        public static List<ServiceTask> GetModuleServices(
            string auth,
            string id)
        {
            List<ServiceTask> moduleServices = new List<ServiceTask>();
            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{id}" } },
                    new { name = "Typetree", value = new [] { SERVICEITEM } }
                }
            });

            var searchResult = Search(auth, query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr,HasChildren,SERVICETYPE, SERVICECATEGORY,SERVICESEQUENCE,SERVICEDURATION");
            var ibs = JObject.Parse(searchResult);
            if (ibs["result"]["dto"].Count() > 0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var x = ib["baskets"][0];
                    var service = new ServiceTask()
                    {
                        Id = x.GetStringValue("DocID"),
                        Rev = x.GetStringValue("DocRev"),
                        DocDescr = x.GetStringValue("DocDescr"),
                        HasChildren = x.GetStringValue("HasChildren"),
                        ServiceType = x.GetStringValue("SERVICETYPE"),
                        ServiceCategory = x["fields"][5]["value"][0].ToString(),
                        ServiceSequence = x.GetStringValue("SERVICESEQUENCE"),
                        ServiceDuration = x.GetStringValue("SERVICEDURATION")
                    };
                    moduleServices.Add(service);

                    if(service.HasChildren == "true")
                    {
                        service.Items = GetServiceItem(auth, service.Id);
                    }
                }
            }

            return moduleServices;
        }

        public static List<ServiceItem> GetServiceItem(
            string auth,
            string id)
        {
            List<ServiceItem> items = new List<ServiceItem>();
            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{id}" } }
                }
            });

            var searchResult = Search(auth, query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr,QTY,SEARCHNAME_EN");
            var ibs = JObject.Parse(searchResult);
            if (ibs["result"]["dto"].Count() > 0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var x = ib["baskets"][0];
                    var item = new ServiceItem()
                    {
                        ItemNumber = x.GetStringValue("DocID"),
                        ItemName = x.GetStringValue("DocDescr"),
                        Description = x.GetStringValue("SEARCHNAME_EN"),
                        Qty = x.GetStringValue("QTY")
                    };
                    items.Add(item);
                }
            }

            return items;
        }

    }
}

