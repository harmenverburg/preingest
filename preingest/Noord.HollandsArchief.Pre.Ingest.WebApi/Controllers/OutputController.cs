using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutputController : ControllerBase
    {
        private readonly ILogger<OutputController> _logger;
        private readonly AppSettings _settings = null;
        private readonly CollectionHandler _preingestHandler = null;
        public OutputController(ILogger<OutputController> logger, IOptions<AppSettings> settings, CollectionHandler preingestHandler)
        {
            _logger = logger;
            _settings = settings.Value;
            _preingestHandler = preingestHandler;
        }

        [HttpGet("collections", Name = "Get collections of tar/tar.gz files.", Order = 0)]
        public IActionResult GetCollections()
        {
            dynamic dataResults = null;            
            try
            {
                dataResults = _preingestHandler.GetCollections();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally
            {
                _logger.LogInformation("Exit UpdateProcessAction.");
            }
            
            if (dataResults == null)
                return NotFound("Not collections data found!");
            
            return new JsonResult(dataResults);  
        }

        [HttpGet("collection/{guid}", Name = "Get specific collection of tar/tar.gz file.", Order = 1)]
        public IActionResult GetCollection(Guid guid)
        {
            var directory = new DirectoryInfo(_settings.DataFolderName);

            if (!directory.Exists)
                return Problem(String.Format("Data folder '{0}' not found!", _settings.DataFolderName));

            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            dynamic dataResults = null;

            try
            {
                dataResults = _preingestHandler.GetCollection(guid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally
            {
                _logger.LogInformation("Exit UpdateProcessAction.");
            }

            if (dataResults == null)
                return NotFound(String.Format("Not data found for collection '{0}'!", guid));

            return new JsonResult(dataResults);
        }

        [HttpGet("json/{guid}/{json}", Name = "Get json results from a session.", Order = 2)]
        public IActionResult GetJson(Guid guid, string json)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            if (String.IsNullOrEmpty(json))
                return Problem("Json file name is empty.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, guid.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            var fileinfo = directory.GetFiles(json).First();
            if (fileinfo == null)
                return Problem(String.Format("File in session guid '{0}' not found!", json));

            string content = System.IO.File.ReadAllText(fileinfo.FullName);
            
            var result = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")                
            };
            return new RandomJsonResponseMessageResult(result);           
        }

        [HttpGet("report/{guid}/{file}", Name = "Get a report as a file from a session.", Order = 3)]
        public IActionResult GetReport(Guid guid, string file)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            if (string.IsNullOrEmpty(file))
                return Problem("File name is empty.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            var fileinfo = directory.GetFiles(file).First();
            if (fileinfo == null)
                return Problem(String.Format("File in session guid '{0}' not found!", file));

            string contentType = String.Empty;

            switch (fileinfo.Extension)
            {
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".xml":
                    contentType = "text/xml";
                    break;
                case ".csv":
                    contentType = "text/csv";
                    break;
                case ".json":
                    contentType = "application/json";
                    break;
                default:
                    contentType = "application/octet-stream";
                    break;
            }

            return new PhysicalFileResult(fileinfo.FullName, contentType);
        }

    }
}
