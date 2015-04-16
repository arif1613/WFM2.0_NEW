using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IntegrationServicesHub.Models.Util.ValueObjects;
using IntegrationServicesHub.RavenDBConfig;
using Microsoft.AspNet.SignalR;

namespace IntegrationServicesHub.Controllers.Hubs
{
    public class ContentService : Hub
    {
        public string AddContent(ContentData cd)
        {
            try
            {
                var ravenDbCommunication = new RavenDbCommunication("ContentServiceDB");
                var session = ravenDbCommunication.DocumentStore.OpenSession();
                session.Store(cd);
                session.SaveChanges();
                return "Content added successfully in MPP5(RavenDb)...";
            }
            catch (Exception e)
            {
                return e.InnerException.ToString();
            }
        }

        public List<ContentData> SearchContentByObjectID(UInt64 ObjectId)
        {
            try
            {
                var ravenDbCommunication = new RavenDbCommunication("ContentServiceDB");
                var session = ravenDbCommunication.DocumentStore.OpenSession();
                var v=session.Query<ContentData>().Where(r=>r.ObjectID==ObjectId).ToList();
                return v;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}