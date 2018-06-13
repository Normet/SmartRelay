using System.Collections.Generic;
using System.Web.Http;

namespace Normet.Cloud.Relay
{
    public class SoveliaController : ApiController
    {
        public IHttpActionResult GetAllAssets()
        {
            SoveliaApi api = new SoveliaApi("era", "era1234");
            var assets = api.GetAllAssets();

            if (assets == null)
            {
                return NotFound();
            }

            return Ok(assets);
        }
    }
}
