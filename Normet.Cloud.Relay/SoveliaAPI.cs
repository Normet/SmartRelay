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
        string username, password, baseuri;
        JObject auth;

        public SoveliaApi()
        {
            throw new Exception("Do not use default constructor");
        }

        public SoveliaApi(
            string _username,
            string _password,
            string _protocol = "http",
            string _hostname = "fi-sov-test",
            string _port = "8080",
            string _basepath = "/auric/api/rest")
        {
            username = _username;
            password = _password;
            baseuri = $"{_protocol}://{_hostname}:{_port}{_basepath}";
        }

        public string Login(
            string username,
            string password,
            string protocol = "http",
            string hostname = "fi-sov-test",
            string port = "8080",
            string basepath = "/auric/api/rest")
        {
            var client = new RestClient($"{protocol}://{hostname}:{port}{basepath}/user/login");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("undefined", string.Format($"username={username}&password={password}&longsession=true"), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);


            auth = JObject.Parse(response.Content);
            if(auth.ContainsKey("httpCode"))
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

        public string Search(string criterias)
        {
            return Search(criterias, $"{auth["token"]}", $"{ baseuri + "/search?minbasket=1"}");
        }

        public string RevisedIBs(string revDate = "[-7d]-*")
        {
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

            var searchResult = Search(query.ToString());

            var ibs = JObject.Parse(searchResult);
            if(ibs["result"]["dto"].Count()>0)
            {
                foreach (var field in ibs["result"]?["dto"]?["objects"]?[0].SelectTokens("$..fields[?(@.name=='CUSTOMER' || @.name=='BASEITEMCODE')]"))
                {
                    

                }
            }

            return searchResult;
        }
        public string IBModules(string ibDocId)
        {
            JObject query = JObject.FromObject(new
            {
                criterias = new[]
                {
                    new { name = "linkto", value = new []{ $"{ibDocId}" } },
                    new { name = "Typetree", value = new [] { "Service,Service Item" } }
                }
            });

            return Search(query.ToString());
        }


    }
}
