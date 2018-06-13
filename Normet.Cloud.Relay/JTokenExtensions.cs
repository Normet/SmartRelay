using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Normet.Cloud.Relay
{
    public static class JTokenExtensions
    {
        public static string GetStringValue(this JToken token, string fieldname)
        {
            string ret = "";
            var v = token.SelectToken($"$..fields[?(@.name=='{fieldname}')].value[0]");
            if (v != null)
            {
                ret = v.ToString();
            }
            return ret;
        }
    }
}
