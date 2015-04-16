using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using IntegrationServicesHub.Models.HubModels;
using IntegrationServicesHub.Models.Util.ValueObjects;
using IntegrationServicesHub.RavenDBConfig;

namespace IntegrationServicesHub.Controllers
{
    public class ServiceProvidersController : Controller
    {
        private static RavenDbCommunication ravenDbCommunication { get; set; }

        public ServiceProvidersController()
        {
             ravenDbCommunication = new RavenDbCommunication("ServiceConfigurationDB");
        }

        // GET: ServiceProviders
        public ActionResult Index()
        {
            var session = ravenDbCommunication.DocumentStore.OpenSession();
            var result = session.Query<ServiceProvidersModel>().ToList();
            return View(result);
        }

        // GET: ServiceProviders/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ServiceProviders/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ServiceProviders/Create
        [HttpPost]
        public ActionResult Create(ServiceProvidersModel serviceProvidersModel)
        {
            try
            {
                var session = ravenDbCommunication.DocumentStore.OpenSession();
                session.Store(serviceProvidersModel);
                session.SaveChanges();
                Thread.Sleep(1500);
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ServiceProviders/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ServiceProviders/Edit/5
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
                var v = session.Query<ServiceProvidersModel>().Where(r => r.ID == id).FirstOrDefault();
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
