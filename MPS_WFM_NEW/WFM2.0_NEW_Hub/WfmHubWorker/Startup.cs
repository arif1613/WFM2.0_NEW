using System.Web.Http;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(WfmHubWorker.Startup))]

namespace WfmHubWorker
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                 "Default",
                 "{controller}/{id}",new { id = RouteParameter.Optional });

            app.UseWebApi(config);
        }
    }
}
