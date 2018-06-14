using Owin;
using Microsoft.Owin.Hosting;
using System;
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Normet.Cloud.Relay
{
    public class BrowserJsonFormatter: JsonMediaTypeFormatter
    {
        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = new MediaTypeHeaderValue("application/json");            
        }
    }

    public class RelayService
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/api/sovelia",map => {
                var config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();
                config.EnsureInitialized();
                map.UseWebApi(config);
            });
        }
    }

    public class RelayServiceHost
    {
        private IDisposable app;
        public void Start()
        {
            string baseAddress = "http://localhost:9000/";

            
            app = WebApp.Start<RelayService>(url: baseAddress);
        }

        public void Stop()
        {
            app.Dispose();
        }
    }
}
