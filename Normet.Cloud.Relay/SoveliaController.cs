using System.Collections.Generic;
using System.Web.Http;

namespace Normet.Cloud.Relay
{
    public class SoveliaBaseRequest
    {
        public string Hostname { get; set; }
    }
    public class SoveliaAssetRequest: SoveliaBaseRequest
    {
        public string RevDate { get; set; }
    }
    public class SoveliaModuleRequest : SoveliaBaseRequest
    {
        public string AssetId { get; set; }
    }

    public class SoveliaController : ApiController
    {
        [Route("assets")]
        [HttpPost]
        public IHttpActionResult ListAssets([FromBody] SoveliaAssetRequest request)
        {
            var api = new SoveliaApi(request.Hostname);
            var assets = api.GetAllAssets(request.RevDate);

            if(assets == null)
            {
                return NotFound();
            }

            return Ok(assets);
        }

        [Route("assets/{id}/modules")]
        public IHttpActionResult ListModules([FromBody] SoveliaModuleRequest request, string id)
        {
            var api = new SoveliaApi(request.Hostname);
            var modules = api.GetAssetModules(id);

            if (modules == null)
            {
                return NotFound();
            }

            return Ok(modules);
        }

    }
}
