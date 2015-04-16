using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(IntegrationServicesHub.Startup))]
namespace IntegrationServicesHub
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var hubConfig = new HubConfiguration();
            hubConfig.EnableDetailedErrors = true;
            app.MapSignalR("/signalr", hubConfig);
            ConfigureAuth(app);
        }

    }
}
