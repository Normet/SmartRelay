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
        string username, password, baseuri, token;
        JObject auth;

        #region Constructors
        public SoveliaApi()
        {
            throw new Exception("Do not use default constructor");
        }
        public SoveliaApi(
            string _uri)
        {
            username = "era";
            password = "era1234";
            baseuri = $"http://{_uri}/auric/api/rest";
            Login(username, password, baseuri);
        }
        #endregion

        #region Login
        public string Login(
            string username,
            string password,
            string uri
            )
        {
            var client = new RestClient($"{uri}/user/login");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("undefined", string.Format($"username={username}&password={password}&longsession=true"), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            auth = JObject.Parse(response.Content);
            if (auth.ContainsKey("httpCode"))
            {
                var httpCode = (string)auth["httpCode"];
                switch ($"{auth["httpCode"]}")
                {
                    case "200":
                        auth.Add("token", $"{response.Headers.FirstOrDefault(i => i.Name == "X-Access-Token").Value}");
                        auth.Add("dateTime", DateTime.Now);
                        auth.Add("cookies", JArray.FromObject(response.Cookies));
                        auth.Add("uri", $"{baseuri + "/user/login"}");
                        break;
                }
            }
            return auth.ToString();
        }
        #endregion

        #region Search
        public string Search(
            string criterias)
        {
            return Search(criterias, $"{auth["token"]}", $"{ baseuri + "/search?minbasket=1"}");
        }
        public string Search(
            string criterias,
            string basket)
        {
            return Search(criterias, $"{auth["token"]}", $"{ baseuri + "/search?" + basket}");
        }
        public string Search(
            string criterias,
            string token,
            string uri)
        {
            var client = new RestClient($"{uri}");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("X-Access-Token", $"{token}");
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

        public List<CustomerAsset> GetAllAssets(
            string revDate = "[-7d]-*",
            bool cascade = false)
        {
            List<CustomerAsset> customerAssetList = new List<CustomerAsset>();
            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "Typetree", value = new []{ "Installed base management,Equipment" } },
                    new { name = "HasParents", value = new [] { "false" } },
                    new { name = "HasChildren", value = new [] { "true" } },
                    new { name = "StatLatestRel", value = new [] { "true" } },
                    new { name = "LastRev", value = new [] { "true" } },
                    new { name = "RevDate", value = new [] { $"{revDate}" } }
                }
            });

            var searchResult = Search(query.ToString(), "minbasket=6&superbasket=CUSTOMER,BASEITEMCODE,BASEITEMREVISION,DocID,MODEL,CUSTOMERSERIALNUMBER,SERIALNUMBER,REALENGINEHOURS,CURRENTCUSTOMER,MINEORTUNNELSITE,STARTUPDATE");

            var ibs = JObject.Parse(searchResult);
            if(ibs["result"]["dto"].Count()>0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var customerAsset = new CustomerAsset() {
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
                        StartupDate = ib.GetStringValue("STARTUPDATE")
                    };
                    if (cascade)
                    {
                        customerAsset.Modules = GetAssetModules(customerAsset.DocId);
                    }
                    customerAssetList.Add(customerAsset);
                }
            }
            return customerAssetList;
        }

        public List<AssetModule> GetAssetModules(
            string id)
        {
            var assetModules = new List<AssetModule>();
            var asset = new AssetModule()
            {
                ItemNumber = id,
                Description = "General maintenance service module"
            };
            asset.AddServiceTaskRange(GetModuleServices(id));
            assetModules.Add(asset);

            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{id}" } },
                    new { name = "Typetree", value = new [] { "Service Module" } }
                }
            });

            var searchResult = Search(query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr");
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
                    asset.AddServiceTaskRange(GetSubModuleServices(docId));
                    assetModules.Add(asset);
                }
            }

            return assetModules;
        }

        public List<ServiceTask> GetSubModuleServices(
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

            var searchResult = Search(query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr,HasChildren");
            var ibs = JObject.Parse(searchResult);
            if (ibs["result"]["dto"].Count() > 0)
            {
                foreach (var ib in ibs["result"]?["dto"]?["objects"])
                {
                    var x = ib["baskets"][0];
                    string docid = x.GetStringValue("DocID");                    

                    serviceTasks.AddRange(GetModuleServices(docid));
                }
            }

            return serviceTasks;
        }

        public List<ServiceTask> GetModuleServices(
            string id)
        {
            List<ServiceTask> moduleServices = new List<ServiceTask>();
            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{id}" } },
                    new { name = "Typetree", value = new [] { "Service Item" } }
                }
            });

            var searchResult = Search(query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr,HasChildren,SERVICETYPE, SERVICECATEGORY,SERVICESEQUENCE,SERVICEDURATION");
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
                        ServiceCategory = x.GetStringValue("SERVICECATEGORY"),
                        ServiceSequence = x.GetStringValue("SERVICESEQUENCE"),
                        ServiceDuration = x.GetStringValue("SERVICEDURATION")
                    };
                    moduleServices.Add(service);

                    if(service.HasChildren == "true")
                    {
                        service.Items = GetServiceItem(service.Id);
                    }
                }
            }

            return moduleServices;
        }

        public List<ServiceItem> GetServiceItem(
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

            var searchResult = Search(query.ToString(), "minbasket=6&superbasket=DocID,DocRev,DocDescr,QTY,SEARCHNAME_EN");
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

