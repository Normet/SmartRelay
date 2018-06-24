using Owin;
using Microsoft.Owin.Hosting;
using System;
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Configuration;

namespace Normet.Cloud.Relay
{
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
            string baseAddress = $"http://{ConfigurationManager.AppSettings["ServiceAddress"]}:{ConfigurationManager.AppSettings["ServicePort"]}/";            
            app = WebApp.Start<RelayService>(url: baseAddress);
        }

        public void Stop()
        {
            app.Dispose();
        }
    }
}
