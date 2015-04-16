using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Raven.Client.Document;

namespace IntegrationServicesHub.RavenDBConfig
{
    public class RavenDbCommunication
    {
        public string ravenDbUrl { get; set; }
        public string RavenDbName { get; set; }
        public DocumentStore DocumentStore { get; set; }

        public RavenDbCommunication(string dbName)
        {
            ravenDbUrl = "http://localhost:8080";
            RavenDbName = dbName;
            DocumentStore = new DocumentStore
            {
                Url = ravenDbUrl,
                DefaultDatabase = RavenDbName
            };
            DocumentStore.Initialize();
        }


    }
}