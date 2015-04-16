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
    public class ContentsRightsOwnerController : Controller
    {
        private static RavenDbCommunication ravenDbCommunication { get; set; }

        public ContentsRightsOwnerController()
        {
             ravenDbCommunication = new RavenDbCommunication("ContentRightOwnersDB");
        }

        public ActionResult Index()
        {
            var session = ravenDbCommunication.DocumentStore.OpenSession();
            var result = session.Query<ContentRightsOwner>().ToList();
            return View(result);
        }

        // GET: ContentsRightsOwner/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ContentsRightsOwner/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContentsRightsOwner/Create
        [HttpPost]
        public ActionResult Create(ContentRightsOwner cro)
        {
            try
            {
                var session = ravenDbCommunication.DocumentStore.OpenSession();
                session.Store(cro);
                session.SaveChanges();
                Thread.Sleep(1500);
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ContentsRightsOwner/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ContentsRightsOwner/Edit/5
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
                var v = session.Query<ContentRightsOwner>().Where(r => r.ID == id).FirstOrDefault();
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
