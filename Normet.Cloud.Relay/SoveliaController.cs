using System.Collections.Generic;
using System.Web.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System;

namespace Normet.Cloud.Relay
{

    public class MemoryCacher
    {
        public object GetValue(string key)
        {
            MemoryCache memoryCache = MemoryCache.Default;
            return memoryCache.Get(key);
        }
        public bool Add(string key, object value, DateTimeOffset absExpiration)
        {
            MemoryCache memoryCache = MemoryCache.Default;
            return memoryCache.Add(key, value, absExpiration);
        }
        public void Delete(string key)
        {
            MemoryCache memoryCache = MemoryCache.Default;
            if(memoryCache.Contains(key))
            {
                memoryCache.Remove(key);
            }
        }

    }
    public class SoveliaController : ApiController
    {
        MemoryCacher cache = new MemoryCacher();

        [Route("login")]
        [HttpPost]
        public IHttpActionResult Login([FromBody] SoveliaLoginRequest request)
        {
            return Ok(SoveliaApi.Login(request.Username, request.Password, request.Hostname));
        }

        [Route("assets")]
        [HttpPost]
        public IHttpActionResult ListAssets([FromBody] SoveliaAssetRequest request)
        {
            List<CustomerAsset> assets;
            if(request.UseCache 
                && cache.GetValue($"{request.RevDate}+{request.Cascade}") != null)
            {
                assets = (List<CustomerAsset>)cache.GetValue($"{request.RevDate}+{request.Cascade}");
            }
            else
            {
                assets = SoveliaApi.GetAllAssets(request.Auth, request.RevDate, request.Cascade);
                cache.Add($"{request.RevDate}+{request.Cascade}", assets, DateTimeOffset.Now.AddHours(8));
            }
            return Ok(assets);
        }

        [Route("search")]
        [HttpPost]
        public IHttpActionResult Search([FromBody] SoveliaSearchRequest request)
        {
            return Ok(SoveliaApi.Search(request.Auth, request.Criteria, request.Basket));
        }
    }

    #region Request model
    public class SoveliaBaseRequest
    {
        public string Auth { get; set; }
        public bool UseCache { get; set; }
    }
    public class SoveliaLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Hostname { get; set; }
    }
    public class SoveliaAssetRequest : SoveliaBaseRequest
    {
        public string RevDate { get; set; }
        public bool Cascade { get; set; }
    }
    public class SoveliaSearchRequest : SoveliaBaseRequest
    {
        public string Criteria { get; set; }
        public string Basket { get; set; }
    }

    #endregion
}
