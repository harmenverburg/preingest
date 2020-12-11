using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebTool.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebTool.Models;

namespace Noord.HollandsArchief.Pre.Ingest.WebTool.Controllers
{
    public class SidecarController : Controller
    {
        private readonly ILogger<SidecarController> _logger;
        
        public SidecarController(ILogger<SidecarController> logger, PreingestModel model)
        {
            _logger = logger;
            Model = model;
        }

        PreingestModel Model { get; set; }

        public IActionResult Index(Guid? id)
        {
            if (id.HasValue)
            {
                dynamic result = Model.GetSidecarTree(id.Value);
                ViewBag.JsonTree = result;
                ViewBag.CurrentSession = id.Value;
            }

            var collections = Model.GetCollections();
            var sessions = Model.GetSessions();

            dynamic model = new IndexViewModel { Collections = collections, Sessions = sessions };
            return View(model); 
        }

        public IActionResult UpdateBinary(Guid id)
        {
            dynamic response = Model.UpdateBinary(id);
            ViewBag.CurrentSession = id;
            return Redirect("~/Sidecar/Index");
        }

        public IActionResult GenerateExport(Guid id)
        {
            var response = Model.GenerateExport(id);
            ViewBag.CurrentSession = id;


            return Redirect("~/Sidecar/Index");
        }

        public IActionResult Export(Guid id)
        {
            return Redirect("~/Sidecar/Index");
        }

        public IActionResult PlanetSummary(Guid id)
        {            
            XDocument planetsReport = null;

            try
            {
                dynamic result = Model.GetDroidAndPlanetSummary(id);             
                string planetsXml = result.planets.ToString();

                if (!String.IsNullOrEmpty(planetsXml))
                    planetsReport = XDocument.Parse(planetsXml);
            }
            catch
            {
                return PartialView("~/Views/Sidecar/Summary/ErrorMessage.cshtml", new JsonResult(new { Message = "Planets XML parse is niet gelukt!" }));
            }

            return PartialView("~/Views/Sidecar/Summary/Planets.cshtml", planetsReport);
        }

        public IActionResult DroidSummary(Guid id)
        {
            XDocument droidReport = null;

            try
            {
                dynamic result = Model.GetDroidAndPlanetSummary(id);

                string droidXml = result.droid.ToString();

                if (!String.IsNullOrEmpty(droidXml))
                    droidReport = XDocument.Parse(droidXml);
            }
            catch
            {
                return PartialView("~/Views/Sidecar/Summary/ErrorMessage.cshtml", new JsonResult(new { Message = "Droid XML parse is niet gelukt!" }));
            }

            return PartialView("~/Views/Sidecar/Summary/Droid.cshtml", droidReport);
        }
        
        public IActionResult AggregationSummary(Guid id)
        {
            dynamic result = Model.GetAggregationSummary(id);
            ViewBag.CurrentSession = id;
            return PartialView("~/Views/Sidecar/Summary/Aggregation.cshtml", result);
        }

        public IActionResult VirusscanSummary(Guid id)
        {
            dynamic result = Model.GetVirusscanResult(id);

            if (!(result is List<ProcessResult>))            
                result = null;            

            ViewBag.CurrentSession = id;
            return PartialView("~/Views/Sidecar/Summary/Virusscan.cshtml", result);
        }

        public IActionResult NamingSummary(Guid id)
        {
            dynamic result = Model.GetNamingCheckResult(id);

            if (!(result is List<ProcessResult>))
                result = null;

            ViewBag.CurrentSession = id;
            return PartialView("~/Views/Sidecar/Summary/Naming.cshtml", result);
        }

        public IActionResult TopxContent(Guid sessionId, Guid treeId)
        {
            dynamic result = Model.GetTopxData(sessionId, treeId);
            string html = string.Empty;
            if(result != null && result.topx != null)            
                html = result.topx;
            
            ViewBag.CurrentSession = sessionId;
            ViewBag.CurrentTreeItem = treeId;
            return PartialView("~/Views/Sidecar/Properties/Topx.cshtml", html);
        }

        public IActionResult DroidPronomInfo(Guid sessionId, Guid treeId)
        {
            List<dynamic> result = Model.GetDroidPronomInfo(sessionId, treeId);   
            ViewBag.CurrentSession = sessionId;
            ViewBag.CurrentTreeItem = treeId;
            return PartialView("~/Views/Sidecar/Properties/Pronom.cshtml", result);
        }

        public IActionResult MetadataEncoding(Guid sessionId, Guid treeId)
        {
            dynamic result = Model.GetMetadataEncoding(sessionId, treeId);
            ViewBag.CurrentSession = sessionId;
            ViewBag.CurrentTreeItem = treeId;
            return PartialView("~/Views/Sidecar/Properties/Encoding.cshtml", result);
        }

        public IActionResult GreenlistStatus(Guid sessionId, Guid treeId)
        {
            dynamic result = Model.GetGreenlistStatus(sessionId, treeId);
            ViewBag.CurrentSession = sessionId;
            ViewBag.CurrentTreeItem = treeId;
            return PartialView("~/Views/Sidecar/Properties/Greenlist.cshtml", result);
        }

        public IActionResult Checksums(Guid sessionId, Guid treeId)
        {
            dynamic result = Model.GetChecksums(sessionId, treeId);
            ViewBag.CurrentSession = sessionId;
            ViewBag.CurrentTreeItem = treeId;
            return PartialView("~/Views/Sidecar/Properties/Checksum.cshtml", result);
        }

        public IActionResult SchemaResult(Guid sessionId, Guid treeId)
        {
            List<ProcessResult> result = Model.GetSchemaResult(sessionId, treeId);
            ViewBag.CurrentSession = sessionId;
            ViewBag.CurrentTreeItem = treeId;
            return PartialView("~/Views/Sidecar/Properties/Schema.cshtml", result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
