using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreingestController : ControllerBase
    {
        private readonly ILogger<PreingestController> _logger;
        private AppSettings _settings = null;
        public PreingestController(ILogger<PreingestController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpGet("check", Name = "API service check", Order = 0)]
        public IActionResult Check()
        {
            HealthCheckHandler handler = HttpContext.RequestServices.GetService(typeof(HealthCheckHandler)) as HealthCheckHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(HealthCheckHandler).Name);

            handler.Execute();

            return new JsonResult(new ProcessResult(Guid.NewGuid())
            {
                CollectionItem = string.Empty,
                Code = "Underlying services health check.",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = new string[] {
                    "preingest: available",
                    String.Format("clamav: {0}", handler.IsAliveClamAv ? "available" : "not available"),
                    String.Format("xslweb: {0}", handler.IsAliveXslWeb ? "available" : "not available"),
                    String.Format("droid: {0}", handler.IsAliveDroid ? "available" : "not available")
                }
            });
        }

        [HttpGet("calculate/{checksum}/{collectionName}", Name = "Collection checksum calculation. Options : MD5, SHA1, SHA256, SHA512", Order = 1)]
        public IActionResult CollectionChecksumCalculation(String checksum, String collectionName)
        {
            _logger.LogInformation("Enter CollectionChecksumCalculation.");

            if (String.IsNullOrEmpty(collectionName))
                return BadRequest("Missing collection name.");
            
            if (String.IsNullOrEmpty(checksum))
                return BadRequest("Missing checksum name.");

            bool exists = System.IO.File.Exists(Path.Combine(_settings.DataFolderName, collectionName));
            if (!exists)
                return NotFound("Collection not found.");            

            ContainerChecksumHandler handler = HttpContext.RequestServices.GetService(typeof(ContainerChecksumHandler)) as ContainerChecksumHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(ContainerChecksumHandler).Name);

            handler.Logger = _logger;
            handler.TarFilename = collectionName;
            handler.Checksum = checksum;

            Guid guid = Guid.NewGuid();
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(ContainerChecksumHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(ContainerChecksumHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(ContainerChecksumHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(ContainerChecksumHandler).Name);
            }

            _logger.LogInformation("Exit CollectionChecksumCalculation.");
            return new JsonResult(new { Message = String.Format("Container checksum calculation {0} is started", collectionName), SessionId = guid });
        }

        //Voorbereiding  
        [HttpGet("unpack/{collectionName}", Name = "Unpack tar Collection", Order = 2)]
        public IActionResult Unpack(String collectionName)
        {
            _logger.LogInformation("Enter Unpack.");

            if (String.IsNullOrEmpty(collectionName))
                return BadRequest("Missing collection name.");

            bool exists = System.IO.File.Exists(Path.Combine(_settings.DataFolderName, collectionName));
            if (!exists)
            {
                return NotFound("Collection not found.");
            }

            UnpackTarHandler handler = HttpContext.RequestServices.GetService(typeof(UnpackTarHandler)) as UnpackTarHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(UnpackTarHandler).Name);

            handler.Logger = _logger;
            handler.TarFilename = collectionName;
            Guid guid = Guid.NewGuid();
            handler.SetSessionGuid(guid);    
            
            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(UnpackTarHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(UnpackTarHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                }); 
            }
            catch(Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(UnpackTarHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(UnpackTarHandler).Name);
            }

            _logger.LogInformation("Exit Unpack.");
            return new JsonResult(new { Message = String.Format ("Unpack tar container '{0}' started", collectionName), SessionId = guid });
        }

        //Check 1 : virus scannen
        [HttpPost("virusscan/{guid}", Name = "Virusscan check", Order = 3)]
        public IActionResult VirusScan(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter VirusScan.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format ("Session {0} not found.", guid));
            }

            ScanVirusValidationHandler handler = HttpContext.RequestServices.GetService(typeof(ScanVirusValidationHandler)) as ScanVirusValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(ScanVirusValidationHandler).Name);
            
            handler.Logger = _logger;
            handler.SetSessionGuid(guid);
            
            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(ScanVirusValidationHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(ScanVirusValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(ScanVirusValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(ScanVirusValidationHandler).Name);
            }

            _logger.LogInformation("Exit VirusScan.");
            return new JsonResult(new { Message = String.Format("Virusscan started."), SessionId = guid });
        }
                
        //Check 3 : bestandsnamen en mapnamen 
        [HttpPost("naming/{guid}", Name = "Naming check", Order = 4)]
        public IActionResult Naming(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Naming.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            NamingValidationHandler handler = HttpContext.RequestServices.GetService(typeof(NamingValidationHandler)) as NamingValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(NamingValidationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(NamingValidationHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(NamingValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(NamingValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(NamingValidationHandler).Name);
            }

            _logger.LogInformation("Exit Naming.");
            return new JsonResult(new { Message = String.Format("Folder(s) and file(s) naming check started."), SessionId = guid });
        }
       
        //Check 4 : sidecar structuur
        [HttpPost("sidecar/{guid}", Name = "Sidecar check", Order = 5)]
        public IActionResult Sidecar(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Sidecar.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            SidecarValidationHandler handler = HttpContext.RequestServices.GetService(typeof(SidecarValidationHandler)) as SidecarValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(SidecarValidationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(SidecarValidationHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(SidecarValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(SidecarValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(SidecarValidationHandler).Name);
            }
            _logger.LogInformation("Exit Sidecar.");
            return new JsonResult(new { Message = String.Format("Structure sidecar check started."), SessionId = guid });
        }              
 
        [HttpPost("profiling/{guid}", Name = "Droid create profile", Order = 6)]
        public IActionResult Profiling(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Profiling.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                var task = Task.Run(() =>
                {
                    try
                    {
                        var result = handler.GetProfiles().Result;
                        _logger.LogInformation("Profiling is completed.");
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                        _logger.LogInformation("Profiling exiting thread.");
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit Profiling.");

            return new JsonResult(new { Message = String.Format("Droid profiling is started."), SessionId = handler.SessionGuid });
        }

        //Check 2, 7 : integriteit met DROID
        [HttpPost("exporting/{guid}", Name = "Droid exporting result (CSV)", Order = 7)]
        public IActionResult Exporting(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Exporting.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                var task = Task.Run(() =>
                {
                    try
                    {
                        var result = handler.GetExporting().Result;
                        _logger.LogInformation("Exporting is completed.");
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                        _logger.LogInformation("Exporting exiting thread.");
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit Exporting.");

            return new JsonResult(new { Message = String.Format("Droid exporting (CSV) is started."), SessionId = handler.SessionGuid });
        }       

        [HttpPost("reporting/{type}/{guid}", Name = "Droid reporting PDF/Droid (XML)/Planets (XML)", Order = 8)]
        public IActionResult Reporting(Guid guid, String type)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Reporting.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            DroidValidationHandler.ReportingStyle style = DroidValidationHandler.ReportingStyle.Pdf;
            switch (type)
            {
                case "pdf":
                    style = DroidValidationHandler.ReportingStyle.Pdf;
                    break;
                case "droid":
                    style = DroidValidationHandler.ReportingStyle.Droid;
                    break;
                case "planets":
                    style = DroidValidationHandler.ReportingStyle.Planets;
                    break;
                default:
                    return NotFound(String.Format ("Unknown report type {0}", type));
            }

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                var task = Task.Run(() =>
                {
                    try
                    {
                        var result = handler.GetReporting(style).Result;
                        _logger.LogInformation("Reporting is completed.");
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                        _logger.LogInformation("Reporting exiting thread.");
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit Reporting.");

            return new JsonResult(new { Message = String.Format("Droid reporting ({0}) is started.", style), SessionId = handler.SessionGuid });
        }

        [HttpPost("signature/update", Name = "Droid signature update", Order = 9)]
        public IActionResult SignatureUpdate()
        {
            _logger.LogInformation("Enter SignatureUpdate.");

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

            handler.Logger = _logger;
            Guid dummyGuid = Guid.NewGuid();
            handler.SetSessionGuid(dummyGuid);
                       
            try
            {
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                var task = Task.Run(() =>
                {
                    try
                    {
                        var result = handler.SetSignatureUpdate().Result;
                        _logger.LogInformation("SignatureUpdate is completed.");
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                        _logger.LogInformation("SignatureUpdate exiting thread.");
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit SignatureUpdate.");

            return new JsonResult(new { Message = String.Format("Droid signature update check/download is started."), SessionId = dummyGuid });
        }

        [HttpPost("greenlist/{guid}", Name = "Greenlist check", Order = 10)]
        public IActionResult GreenListCheck(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GreenListCheck.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            GreenListHandler handler = HttpContext.RequestServices.GetService(typeof(GreenListHandler)) as GreenListHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(GreenListHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            string greenList = handler.GreenlistLocation();
            if (String.IsNullOrEmpty(greenList))
            {
                //no greenlist found exit;
                return Problem("Greenlist not found/available.", typeof(GreenListHandler).Name);
            }

            string droidCsvFile = handler.DroidCsvOutputLocation();
            if (String.IsNullOrEmpty(droidCsvFile))
            {
                //no greenlist found exit;
                return Problem("Droid CSV result not found/available. Please run Droid first.", typeof(GreenListHandler).Name);
            }

            try
            { 
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(GreenListHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(GreenListHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(GreenListHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(GreenListHandler).Name);
            }
            _logger.LogInformation("Exit GreenListCheck.");
            return new JsonResult(new { Message = String.Format("Greenlist check is started."), SessionId = guid });
        }

        [HttpPost("encoding/{guid}", Name = "Encoding check .metadata files", Order = 11)]
        public IActionResult EncodingCheck(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter EncodingCheck.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            EncodingHandler handler = HttpContext.RequestServices.GetService(typeof(EncodingHandler)) as EncodingHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(EncodingHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(EncodingHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(EncodingHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(EncodingHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(EncodingHandler).Name);
            }
            _logger.LogInformation("Exit EncodingCheck.");
            return new JsonResult(new { Message = String.Format("Encoding UTF-8 .metadata files check is started."), SessionId = guid });
        }

        [HttpPost("validate/{guid}", Name = "Validate .metadata files", Order = 12)]
        public IActionResult ValidateMetadata(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter ValidateMetadata.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            MetadataValidationHandler handler = HttpContext.RequestServices.GetService(typeof(MetadataValidationHandler)) as MetadataValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(MetadataValidationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(MetadataValidationHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(MetadataValidationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(MetadataValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(MetadataValidationHandler).Name);
            }
            _logger.LogInformation("Exit ValidateMetadata.");
            return new JsonResult(new { Message = String.Format("Validate metadata files is started."), SessionId = guid });
        }

        [HttpPost("transform/{guid}", Name = "Transform .metadata files to .xip", Order = 13)]
        public IActionResult TransformXip(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter TransformXip.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)
            {
                return NotFound(String.Format("Session {0} not found.", guid));
            }

            TransformationHandler handler = HttpContext.RequestServices.GetService(typeof(TransformationHandler)) as TransformationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(TransformationHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(TransformationHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(TransformationHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(TransformationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(TransformationHandler).Name);
            }
            _logger.LogInformation("Exit TransformXip.");
            return new JsonResult(new { Message = String.Format("Transforming to XIP started."), SessionId = guid });
        }
        
        [HttpPost("sipcreator/{guid}", Name = "Start to create sip", Order = 14)]
        public IActionResult CreateSip(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter CreateSip.");

            bool exists = System.IO.Directory.Exists(Path.Combine(_settings.DataFolderName, guid.ToString()));
            if (!exists)            
                return NotFound(String.Format("Session {0} not found.", guid));

            SipCreatorHandler handler = HttpContext.RequestServices.GetService(typeof(SipCreatorHandler)) as SipCreatorHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(SipCreatorHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(SipCreatorHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(SipCreatorHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(SipCreatorHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(SipCreatorHandler).Name);
            }

            _logger.LogInformation("Exit CreateSip.");
            return new JsonResult(new { Message = String.Format("Sip Creator is started."), SessionId = guid });
        }

        [HttpPost("updatebinary/{guid}", Name = "Update binary data", Order = 15)]
        public IActionResult UpdateBinary(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter UpdateBinary.");

            var processingFolder = Path.Combine(_settings.DataFolderName, guid.ToString());
            bool exists = System.IO.Directory.Exists(processingFolder);
            if (!exists)
                return NotFound(String.Format("Session {0} not found.", guid));

            UpdateBinaryHandler handler = HttpContext.RequestServices.GetService(typeof(UpdateBinaryHandler)) as UpdateBinaryHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(UpdateBinaryHandler).Name);

            handler.Logger = _logger;
            handler.SetSessionGuid(guid);

            try
            {
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(UpdateBinaryHandler).Name, guid.ToString());
                var task = Task.Run(() =>
                {
                    try
                    {
                        handler.Execute();
                    }
                    catch (Exception innerExc)
                    {
                        _logger.LogError(innerExc, "An exception is throwned in {0}: '{1}'.", typeof(UpdateBinaryHandler).Name, innerExc.Message);
                        //send notification
                    }
                    finally
                    {
                        //send notification
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(UpdateBinaryHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(UpdateBinaryHandler).Name);
            }

            _logger.LogInformation("Exit UpdateBinary.");
            return new JsonResult(new { Message = String.Format("Update binary is started."), SessionId = guid });
        }

    }
}
