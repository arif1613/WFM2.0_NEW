using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using IntegrationServicesHub.Models.Util.ValueObjects;
using IntegrationServicesHub.RavenDBConfig;
using Raven.Client.Document;

namespace IntegrationServicesHub.Controllers
{
    public class ContentServiceController : Controller
    {
        public ActionResult Index()
        {
            var ravenDbCommunication = new RavenDbCommunication("ContentServiceDB");
            var session = ravenDbCommunication.DocumentStore.OpenSession();
            var result = session.Query<ContentData>().ToList();
            return View(result);
        }

        public ActionResult CreateNewContent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateNewContent(ContentData cd)
        {
            var ravenDbCommunication = new RavenDbCommunication("ContentServiceDB");
            var session = ravenDbCommunication.DocumentStore.OpenSession();
            session.Store(cd);
            session.SaveChanges();
            Thread.Sleep(1500);
            return RedirectToAction("Index");
        }

        public ActionResult Delete(UInt64 id)
        {
            var ravenDbCommunication = new RavenDbCommunication("ContentServiceDB");
            var session = ravenDbCommunication.DocumentStore.OpenSession();
            var v = session.Query<ContentData>().Where(r=>r.ID==id).FirstOrDefault();
            session.Delete(v);
            session.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}