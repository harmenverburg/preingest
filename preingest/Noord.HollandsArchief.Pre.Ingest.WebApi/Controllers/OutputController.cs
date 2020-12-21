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

            var files = directory.GetFiles("*.*").Where(s => s.Extension.EndsWith(".tar") || s.Extension.EndsWith(".gz"));

            return new JsonResult(files.OrderByDescending(item
                => item.CreationTime).Select(item
                    => new { item.Name, item.CreationTime, item.LastWriteTime, item.LastAccessTime, item.Length }).ToArray());       
        }

        [HttpGet("sessions", Name = "Get working session(s).", Order = 1)]
        public IActionResult GetSessions()
        {
            var directory = new DirectoryInfo(_settings.DataFolderName);

            if (!directory.Exists)
                return Problem(String.Format("Data folder '{0}' not found!", _settings.DataFolderName));

            var directories = directory.GetDirectories();

            Guid guid = Guid.Empty;
            return new JsonResult(directories.OrderByDescending(item
                => item.CreationTime).Select(item 
                    => item.Name).Where(item 
                        => Guid.TryParse(item, out guid)).ToArray());
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

        [HttpGet("sidecartree/{guid}", Name = "Get sidecar structure from a session.", Order = 5)]
        public async Task<IActionResult> GetSidecar(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, guid.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            List<JsTreeItem> model = new List<JsTreeItem>();
            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if(fileinfo == null)
                {
                    return;
                }

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var archief = sidecarObjects.OfType<Archief>().Select(item => new JsTreeItem
                {
                    Id = item.InternalId.ToString(),
                    Parent = item.Parent == null ? "#" : item.Parent.InternalId.ToString(),
                    Text = String.Format ("{0} - {1}", item.GetType().Name, item.Name),
                    Icon = "/img/archief.png"
                }).ToArray();
                model.AddRange(archief);
                
                var series = sidecarObjects.OfType<Series>().Select(item => new JsTreeItem
                {
                    Id = item.InternalId.ToString(),
                    Parent = item.Parent == null ? "#" : item.Parent.InternalId.ToString(),
                    Text = String.Format("{0} - {1}", item.GetType().Name, item.Name),
                    Icon = "/img/series.png"
                }).ToArray();
                model.AddRange(series);
                
                var record = sidecarObjects.OfType<Record>().Select(item => new JsTreeItem
                {
                    Id = item.InternalId.ToString(),
                    Parent = item.Parent == null ? "#" : item.Parent.InternalId.ToString(),
                    Text = String.Format("{0} - {1}", item.GetType().Name, item.Name),
                    Icon = "/img/record.png"
                }).ToArray();
                model.AddRange(record);
                
                var dossier = sidecarObjects.OfType<Dossier>().Select(item => new JsTreeItem
                {
                    Id = item.InternalId.ToString(),
                    Parent = item.Parent == null ? "#" : item.Parent.InternalId.ToString(),
                    Text = String.Format("{0} - {1}", item.GetType().Name, item.Name),
                    Icon = "/img/dossier.png"
                }).ToArray();
                model.AddRange(dossier);
               
                var bestand = sidecarObjects.OfType<Bestand>().Select(item => new JsTreeItem
                {
                    Id = item.InternalId.ToString(),
                    Parent = item.Parent == null ? "#" : item.Parent.InternalId.ToString(),
                    Text = String.Format("{0} - {1}", item.GetType().Name, item.Name),
                    Icon = "/img/bestand.png"
                }).ToArray();
                model.AddRange(bestand);
                
                var onbekend = sidecarObjects.OfType<NotDefined>().Select(item => new JsTreeItem
                {
                    Id = item.InternalId.ToString("N"),
                    Parent = item.Parent == null ? "#" : item.Parent.InternalId.ToString("N"),
                    Text = String.Format("{0} - {1}", item.GetType().Name, item.Name),
                    Icon = "/img/unknown.png"
                }).ToArray();
                model.AddRange(onbekend);
            });

            await Task.Run(action);

            return new JsonResult(model);
        }

        [HttpGet("aggregationsummary/{guid}", Name = "Get sidecar structure summary from a session.", Order = 6)]
        public async Task<IActionResult> GetAggregationSummary(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, guid.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            JsonResult result = null;

            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var archief = sidecarObjects.OfType<Archief>().Count();
                var series = sidecarObjects.OfType<Series>().Count();
                var record = sidecarObjects.OfType<Record>().Count();
                var dossier = sidecarObjects.OfType<Dossier>().Count();
                var bestand = sidecarObjects.OfType<Bestand>().Count();
                var onbekend = sidecarObjects.OfType<NotDefined>().Count();

                result = new JsonResult(new { Archief = archief, Series = series, Dossier = dossier, Record = record, Bestand = bestand, Onbekend = onbekend });
            });

            await Task.Run(action);

            return result;
        }

        [HttpGet("droidsummary/{guid}", Name = "Get droid summary from a session.", Order = 7)]
        public async Task<IActionResult> GetDroidSummary(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, guid.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            JsonResult result = null;
            Action action = new Action(() =>
            {
                var droidFileinfo = directory.GetFiles("*.droid.xml").FirstOrDefault();
                var planetsFileinfo = directory.GetFiles("*.planets.xml").FirstOrDefault();

                if (droidFileinfo == null || !droidFileinfo.Exists)
                {
                    result = new JsonResult(new { Message = "Bestand niet gevonden! Controleer of XML rapport met Droid is uitgevoerd." });
                    return;
                }
                if (planetsFileinfo == null || !planetsFileinfo.Exists)
                {
                    result = new JsonResult(new { Message = "Bestand niet gevonden! Controleer of XML rapport met Planets is uitgevoerd." });
                    return;
                }

                XDocument droid = null;//XDocument.Load(droidFileinfo.FullName);            
                XDocument planets = null;// XDocument.Load(planetsFileinfo.FullName);
                //sample call: http://localhost:8080/topx2xip?reluri=8401b678-e622-475e-b382-f7c8bfd10346/Provincie%20Noord%20Holland/NL-K343625354-1/539862/539862.metadata
                string droidUri = GetProcessingUrl(_settings.XslWebServerName, _settings.XslWebServerPort, "droid2html", droidFileinfo.FullName);
                string planetsUri = GetProcessingUrl(_settings.XslWebServerName, _settings.XslWebServerPort, "planets2html", droidFileinfo.FullName);
                
                WebRequest droidRequest = WebRequest.Create(droidUri);
                using (WebResponse response = droidRequest.GetResponseAsync().Result)
                    droid = XDocument.Load(response.GetResponseStream());

                WebRequest planetsRequest = WebRequest.Create(planetsUri);
                using (WebResponse response = planetsRequest.GetResponseAsync().Result)
                    planets = XDocument.Load(response.GetResponseStream());

                result = new JsonResult(new { Droid = droid.ToString(), Planets = planets.ToString() });   
            });

            await Task.Run(action);

            return result;
        }

        [HttpGet("topxdata/{id}/{treeGuid}", Name = "Get topx content from a session.", Order = 8)]
        public async Task<IActionResult> GetTopxData(Guid id, Guid treeGuid)
        {
            if (id == Guid.Empty || treeGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, id.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            XDocument metadataDocument = null;
            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)               
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var sidecarObject = sidecarObjects.FirstOrDefault(item => item.InternalId == treeGuid);
                if (sidecarObject != null)
                {
                    var metadataFile = sidecarObject.MetadataFileLocation;
                    string metadataUri = GetProcessingUrl(_settings.XslWebServerName, _settings.XslWebServerPort, "topx2html", metadataFile);

                    WebRequest droidRequest = WebRequest.Create(metadataUri);
                    using (WebResponse response = droidRequest.GetResponseAsync().Result)
                        metadataDocument = XDocument.Load(response.GetResponseStream());
                }
            });
            await Task.Run(action);

            return new JsonResult(new { Topx = metadataDocument != null ? metadataDocument.ToString() : "" });
        }

        [HttpGet("droidpronominfo/{id}/{treeGuid}", Name = "Get droid export file property information from a session.", Order = 9)]
        public async Task<IActionResult> GetDroidPronomInformation(Guid id, Guid treeGuid)
        {
            if (id == Guid.Empty || treeGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, id.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            List<dynamic> result = new List<dynamic>();

            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var sidecarObject = sidecarObjects.FirstOrDefault(item => item.InternalId == treeGuid);

                if (sidecarObject == null)
                { return; }

                if(sidecarObject.PronomMetadataInfo != null)
                {
                    var list = new List<PronomItem>();
                    list.Add(sidecarObject.PronomMetadataInfo);
                    if ((sidecarObject as Bestand) != null)
                    {
                        var pronomBinary = (sidecarObject as Bestand).PronomBinaryInfo;
                        list.Add(pronomBinary);
                    }

                    result.AddRange(list);
                    return;
                }
                
                string droidCsvFile = DroidCsvOutputLocation(Path.Combine(_settings.DataFolderName, id.ToString()));
                if (String.IsNullOrEmpty(droidCsvFile))
                { return; }

                using (var reader = new StreamReader(droidCsvFile))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>();
                        
                        var metadataFile = sidecarObject.MetadataFileLocation;
                        var phyisicalFile = ((sidecarObject as Bestand) != null) ? (sidecarObject as Bestand).BinaryFileLocation : String.Empty; //sidecarObject.MetadataFileLocation.Remove((sidecarObject.MetadataFileLocation.Length - ".metadata".Length), ".metadata".Length);

                        var phyisicalFileProp = records.Where(item
                            => item.TYPE == "File"
                            && (item.FILE_PATH == phyisicalFile || item.FILE_PATH == metadataFile)).Select(item => new
                            {
                                Id = item.ID,
                                ParentId = item.PARENT_ID,
                                Uri = item.URI,
                                FilePath = item.FILE_PATH,
                                Name = item.NAME,
                                Method = item.METHOD,
                                Status = item.STATUS,
                                Size = item.SIZE,
                                Type = item.TYPE,
                                Ext = item.EXT,
                                LastModified = item.LAST_MODIFIED,
                                ExtensionMisMatch = item.EXTENSION_MISMATCH,
                                Hash = item.HASH,
                                FormatCount = item.FORMAT_COUNT,
                                Puid = item.PUID,
                                MimeType = item.MIME_TYPE,
                                FormatName = item.FORMAT_NAME,
                                FormatVersion = item.FORMAT_VERSION
                            });

                        result.AddRange(phyisicalFileProp);
                    }
                }
            
            });

            await Task.Run(action);

            return new JsonResult(result);
        }

        [HttpGet("metadataencoding/{id}/{treeGuid}", Name = "Get the encoding from the metadata file.", Order = 10)]
        public async Task<IActionResult> GetMetadataEncoding(Guid id, Guid treeGuid)
        {
            if (id == Guid.Empty || treeGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, id.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            String result = string.Empty;

            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var sidecarObject = sidecarObjects.FirstOrDefault(item => item.InternalId == treeGuid);

                if (sidecarObject == null)
                { return; }

                if (!String.IsNullOrEmpty(sidecarObject.MetadataEncoding))
                {
                    result = sidecarObject.MetadataEncoding;
                    return;
                }

                var encoding = directory.GetFiles("EncodingHandler*.json").OrderByDescending(item => item.CreationTime).FirstOrDefault();
                if (encoding == null)
                    return;

                var output = JsonConvert.DeserializeObject<List<ProcessResult>>(System.IO.File.ReadAllText(encoding.FullName));
                if (output == null)
                    return;

                var encodingResult = output.Where(item => item.CollectionItem == sidecarObject.MetadataFileLocation).FirstOrDefault();

                result = encodingResult.Message;
            });
            await Task.Run(action);

            return new JsonResult(new { Encoding = result });
        }

        [HttpGet("greenliststatus/{id}/{treeGuid}", Name = "See if the file is in the greenlist.", Order = 11)]
        public async Task<IActionResult> GetGreenlistStatus(Guid id, Guid treeGuid)
        {
            if (id == Guid.Empty || treeGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, id.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            var json = new JsonResult(new { Greenlist = "" });

            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var sidecarObject = sidecarObjects.FirstOrDefault(item => item.InternalId == treeGuid);

                if (sidecarObject == null)
                { return; }

                if((sidecarObject as Bestand).BinaryFileIsInGreenList.HasValue)
                {
                    json = new JsonResult(new { Greenlist = (sidecarObject as Bestand).BinaryFileIsInGreenList.Value });
                    return;
                }

                var greenlist = directory.GetFiles("GreenListHandler*.json").OrderByDescending(item => item.CreationTime).FirstOrDefault();
                if (greenlist == null)
                    return;

                var output = JsonConvert.DeserializeObject<List<dynamic>>(System.IO.File.ReadAllText(greenlist.FullName));
                if (output == null)
                    return;

                var path = String.Format("{0}/{1}{2}", _settings.DataFolderName, id, sidecarObject.TitlePath);
                var greenlistItem = output.FirstOrDefault(item => item.DroidRecord.Location == path);
               
                if (greenlistItem != null)
                    json = new JsonResult(new { Greenlist = greenlistItem.InGreenList.Value });
            });
            await Task.Run(action);

            return json;
        }

        [HttpGet("calculatechecksum/{id}/{treeGuid}", Name = "Calculate checksum MD5, SHA1, SHA256 and SHA512.", Order = 12)]
        public async Task<IActionResult> GetChecksums(Guid id, Guid treeGuid)
        {
            if (id == Guid.Empty || treeGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, id.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            var json = new JsonResult(new { });

            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var sidecarObject = sidecarObjects.FirstOrDefault(item => item.InternalId == treeGuid);

                if (sidecarObject == null)
                { return; }

                if((sidecarObject as Bestand) != null)
                {
                    if ( ((sidecarObject as Bestand).ChecksumResultCollection != null) && ((sidecarObject as Bestand).ChecksumResultCollection.Count > 0) )
                    {
                        var dic = (sidecarObject as Bestand).ChecksumResultCollection;
                        json = new JsonResult(new { Md5 = dic["MD5"], Sha1 = dic["SHA1"], Sha256 = dic["SHA256"], Sha512 = dic["SHA512"] });
                        return;
                    }
                }

                string file = String.Format("{0}/{1}{2}", _settings.DataFolderName, id, sidecarObject.TitlePath);
                if (System.IO.File.Exists(file))
                {
                    var fileInfo = new FileInfo(file);
                    string md5 = ChecksumHelper.CreateMD5Checksum(fileInfo);
                    string sha1 = ChecksumHelper.CreateSHA1Checksum(fileInfo);
                    string sha256 = ChecksumHelper.CreateSHA256Checksum(fileInfo);
                    string sha512 = ChecksumHelper.CreateSHA512Checksum(fileInfo);
                    json = new JsonResult(new { Md5 = md5, Sha1 = sha1, Sha256 = sha256, Sha512 = sha512 });
                }

            });

            await Task.Run(action);

            return json;
        }

        [HttpGet("schemaresult/{id}/{treeGuid}", Name = "Schema validation result.", Order = 13)]
        public async Task<IActionResult> GetSchemaValidation(Guid id, Guid treeGuid)
        {
            if (id == Guid.Empty || treeGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            var directory = new DirectoryInfo(Path.Combine(_settings.DataFolderName, id.ToString()));

            if (!directory.Exists)
                return Problem(String.Format("Data folder with session guid '{0}' not found!", directory.FullName));

            var json = new JsonResult(new { });

            Action action = new Action(() =>
            {
                var fileinfo = directory.GetFiles("*.bin").FirstOrDefault();
                if (fileinfo == null)
                    return;

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);
                var sidecarObjects = sidecar.Flatten().Select(item => item.Data).Reverse().ToList();

                var sidecarObject = sidecarObjects.FirstOrDefault(item => item.InternalId == treeGuid);

                if (sidecarObject == null)
                { return; }

                var result = sidecarObject.ObjectExceptions();
                if (result.Count > 0)
                {
                    var fromObject = new ProcessResult[] { new ProcessResult(id) { Messages = result.Select(item => item.Message).ToArray() } };
                    json = new JsonResult(fromObject);
                    return;
                }

                var schema = directory.GetFiles("MetadataValidationHandler*.json").OrderByDescending(item => item.CreationTime).FirstOrDefault();
                if (schema == null)
                    return;

                var output = JsonConvert.DeserializeObject<List<ProcessResult>>(System.IO.File.ReadAllText(schema.FullName));
                if (output == null)
                    return;

                var filtered = output.Where(item => item.CollectionItem == sidecarObject.MetadataFileLocation).ToList();

                json = new JsonResult(filtered);
            });

            await Task.Run(action);

            return json;
        }

        [HttpGet("genereateexcel/{guid}", Name = "Get the total report for this current guid session.", Order = 0)]
        public IActionResult GenerateReportExport(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GenerateReportExport.");

            //TODO execute call to XSL Web for final report in Excel      

            _logger.LogInformation("Exit GenerateReportExport.");

            return new JsonResult(new { });
        }

        private String DroidCsvOutputLocation(String targetFolder)
        {
            var directory = new DirectoryInfo(targetFolder);
            var files = directory.GetFiles("*.droid.csv");

            if (files.Count() > 0)
            {
                FileInfo droidCsvFile = files.OrderByDescending(item => item.CreationTime).First();
                if (droidCsvFile == null)
                    return null;
                else
                    return droidCsvFile.FullName;
            }
            else
            {
                return null;
            }
        }

        private String GetProcessingUrl(string servername, string port, string action, string pad)
        {
            string reluri = pad.Remove(0, "/data/".Length);
            return String.Format(@"http://{0}:{1}/transform/{2}?reluri={3}", servername, port, action, reluri);
        }

    }
}
