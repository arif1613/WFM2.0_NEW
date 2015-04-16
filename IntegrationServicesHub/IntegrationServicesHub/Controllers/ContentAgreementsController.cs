using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using IntegrationServicesHub.Models.HubModels;
using IntegrationServicesHub.RavenDBConfig;

namespace IntegrationServicesHub.Controllers
{
    public class ContentAgreementsController : Controller
    {
        private static RavenDbCommunication ravenDbCommunication { get; set; }

        public ContentAgreementsController()
        {
             ravenDbCommunication = new RavenDbCommunication("ContentAgreementsDB");
        }

        public ActionResult Index()
        {
            var session = ravenDbCommunication.DocumentStore.OpenSession();
            var result = session.Query<ContentAgreement>().ToList();
            return View(result);
        }

        // GET: ContentAgreements/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ContentAgreements/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContentAgreements/Create
        [HttpPost]
        public ActionResult Create(ContentAgreement ca)
        {
            try
            {
                var session = ravenDbCommunication.DocumentStore.OpenSession();
                session.Store(ca);
                session.SaveChanges();
                Thread.Sleep(1500);
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ContentAgreements/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ContentAgreements/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }


        public ActionResult Delete(UInt64 id)
        {
            try
            {
                var session = ravenDbCommunication.DocumentStore.OpenSession();
                var v = session.Query<ContentAgreement>().Where(r => r.ID == id).FirstOrDefault();
                session.Delete(v);
                session.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
