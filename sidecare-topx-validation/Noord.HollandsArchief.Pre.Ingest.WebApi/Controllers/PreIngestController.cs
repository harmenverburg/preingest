using System;
using System.IO;
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
    public class PreIngestController : ControllerBase
    {
        private readonly ILogger<PreIngestController> _logger;
        private AppSettings _settings = null;
        public PreIngestController(ILogger<PreIngestController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpGet("check", Name = "API service check", Order = 0)]
        public IActionResult Check()
        {
            return new JsonResult(new { Message = String.Format("Status is Ok (running/available).") });
        }

        [HttpGet("calculate/{checksum}/{collectionName}", Name = "Collection checksum calculation. Options : MD5, SHA1, SHA256, SHA512", Order = 1)]
        public IActionResult CollectionChecksumCalculation(String checksum, String collectionName)
        {
            _logger.LogInformation("Enter CollectionChecksumCalculation.");

            if (String.IsNullOrEmpty(collectionName))
                return BadRequest("Missing collection name.");

            bool exists = System.IO.File.Exists(Path.Combine(_settings.DataFolderName, collectionName));
            if(!exists)
            {
                return NotFound("Collection not found.");
            }

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
            return new JsonResult(new { Message = String.Format("Virusscan started.") });
        }
                
        //Check 3 : bestandsnamen en mapnamen 
        [HttpPost("naming/{guid}", Name = "Naming check", Order = 4)]
        public IActionResult Naming(Guid guid)
        {
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
            return new JsonResult(new { Message = String.Format("Folder(s) and file(s) naming check started.") });
        }
       
        //Check 4 : sidecar structuur
        [HttpPost("sidecar/{guid}", Name = "Sidecar check", Order = 5)]
        public IActionResult Sidecar(Guid guid)
        {
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
            return new JsonResult(new { Message = String.Format("Structure sidecar check started.") });
        }              
 
        [HttpPost("profiling/{guid}", Name = "Droid create profile", Order = 6)]
        public IActionResult Profiling(Guid guid)
        {
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
            return new JsonResult(new { Message = String.Format("Greenlist check is started.") });
        }

        [HttpPost("encoding/{guid}", Name = "Encoding check .metadata files", Order = 11)]
        public IActionResult EncodingCheck(Guid guid)
        {
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
            return new JsonResult(new { Message = String.Format("Encoding UTF-8 .metadata files check is started.") });
        }

        [HttpPost("validate/{guid}", Name = "Validate .metadata files", Order = 12)]
        public IActionResult ValidateMetadata(Guid guid)
        {
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
            return new JsonResult(new { Message = String.Format("Validate metadata files is started.") });
        }

        [HttpPost("transform/{guid}", Name = "Transform .metadata files to .xip", Order = 13)]
        public IActionResult TransformXip(Guid guid)
        {
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
            return new JsonResult(new { Message = String.Format("Transforming to XIP started.") });
        }

    }
}
