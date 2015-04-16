using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace IntegrationServicesHub.Controllers.Hubs
{
    public class MppServiceHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }
    }
}