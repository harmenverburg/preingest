using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System.Threading.Tasks;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Net;
using CsvHelper;
using System.Globalization;
using Noord.HollandsArchief.Pre.Ingest.Utilities;
using System.Net.Http;
using System.Text;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutputController : ControllerBase
    {
        private readonly ILogger<OutputController> _logger;
        private AppSettings _settings = null;
        public OutputController(ILogger<OutputController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpGet("collections", Name = "Get collections of tar/tar.gz files.", Order = 0)]
        public IActionResult GetCollections()
        {
            var directory = new DirectoryInfo(_settings.DataFolderName);

            if (!directory.Exists)
                return Problem(String.Format("Data folder '{0}' not found!", _settings.DataFolderName));

            var tarArchives = directory.GetFiles("*.*").Where(s => s.Extension.EndsWith(".tar") || s.Extension.EndsWith(".gz"));

            tarArchives.ToList().ForEach(item =>
            {
                var workingDir = Path.Combine(directory.FullName, ChecksumHelper.GeneratePreingestGuid(item.Name).ToString());
                if (!Directory.Exists(workingDir))
                    directory.CreateSubdirectory(ChecksumHelper.GeneratePreingestGuid(item.Name).ToString());                
            });

            return new JsonResult(tarArchives.OrderByDescending(item
                => item.CreationTime).Select(item
                    => new
                    {
                        Name = item.Name,
                        SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name),
                        CreationTime = item.CreationTime,
                        LastWriteTime = item.LastWriteTime,
                        LastAccessTime = item.LastAccessTime,
                        Size = item.Length
                    }).ToArray()); 
        }

        [HttpGet("sessions", Name = "Get working session(s).", Order = 1)]
        public IActionResult GetSessions()
        {
            var directory = new DirectoryInfo(_settings.DataFolderName);

            if (!directory.Exists)
                return Problem(String.Format("Data folder '{0}' not found!", _settings.DataFolderName));

            var tarArchives = directory.GetFiles("*.*").Where(s => s.Extension.EndsWith(".tar") || s.Extension.EndsWith(".gz"));

            var output = tarArchives.Select(item => new { SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name), Tar = item.Name }).ToArray();

            return new JsonResult(output);
        }

        [HttpGet("results/{guid}", Name = "Get results from a session.", Order = 2)]
        public IActionResult GetResults(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, guid.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            var files = directory.GetFiles();
          
            return new JsonResult(files.OrderByDescending(item 
                => item.CreationTime).Select(item 
                    => item.Name).ToArray());
        }

        [HttpGet("json/{guid}/{json}", Name = "Get json results from a session.", Order = 3)]
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

        [HttpGet("report/{guid}/{file}", Name = "Get a report as a file from a session.", Order = 4)]
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

        [HttpGet("excel/{guid}", Name = "Get the total report for this current guid session.", Order = 5)]
        public IActionResult GenerateReportExport(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GenerateReportExport.");

            //TODO execute call to XSL Web for final report in Excel      

            _logger.LogInformation("Exit GenerateReportExport.");

            return new JsonResult(new { });
        }

    }
}
