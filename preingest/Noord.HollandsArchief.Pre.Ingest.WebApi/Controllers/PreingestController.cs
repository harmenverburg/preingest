using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreingestController : ControllerBase
    {
        private readonly ILogger<PreingestController> _logger;
        private AppSettings _settings = null;
        private readonly IHubContext<PreingestEventHub> _eventHub;

        private void Trigger(object sender, PreingestEventArgs e)
        {
            if ((sender as IPreingest) == null)
                return;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()                    
                    
                },
                Formatting = Formatting.Indented, 
                NullValueHandling = NullValueHandling.Ignore                
            };

            _eventHub.Clients.All.SendAsync(nameof(IEventHub.SendNoticeEventToClient),
                JsonConvert.SerializeObject(new EventHubMessage
                {
                    EventDateTime = e.Initiate,
                    SessionId = e.PreingestAction.Properties.SessionId,
                    Name = e.PreingestAction.Properties.ActionName,
                    State = e.ActionType,
                    Message = e.Description,
                    Summary = e.PreingestAction.Summary
                }, settings)).GetAwaiter().GetResult();

            IPreingest handler = sender as IPreingest;
            if (handler.ActionProcessId == Guid.Empty) return;

            if (e.ActionType == PreingestActionStates.Started)
                handler.AddStartState(handler.ActionProcessId);
            if (e.ActionType == PreingestActionStates.Completed)
                handler.AddCompleteState(handler.ActionProcessId);
            if (e.ActionType == PreingestActionStates.Failed)
            {
                string message = String.Concat(e.PreingestAction.Properties.Messages);
                handler.AddFailedState(handler.ActionProcessId, message);
            }
            if (e.ActionType == PreingestActionStates.Failed || e.ActionType == PreingestActionStates.Completed)
            {
                string result = (e.PreingestAction.ActionResult != null) ? e.PreingestAction.ActionResult.ResultValue.ToString() : PreingestActionResults.None.ToString();
                string summary = (e.PreingestAction.Summary != null) ? JsonConvert.SerializeObject(e.PreingestAction.Summary, settings) : String.Empty;
                handler.UpdateProcessAction(handler.ActionProcessId, result, summary);
            }
        }

        public PreingestController(ILogger<PreingestController> logger, IOptions<AppSettings> settings, IHubContext<PreingestEventHub> eventHub)
        {
            _logger = logger;
            _settings = settings.Value;
            _eventHub = eventHub;
        }

        [HttpGet("check", Name = "API service check", Order = 0)]
        public IActionResult Check()
        {
            HealthCheckHandler handler = HttpContext.RequestServices.GetService(typeof(HealthCheckHandler)) as HealthCheckHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(HealthCheckHandler).Name);

            handler.Execute();

            return new JsonResult(new 
            {                
                Title = "Underlying services health check.",
                CreationTimestamp = DateTimeOffset.Now,
                ActionName = typeof(HealthCheckHandler).Name,
                Messages = new string[] {
                    "preingest: available",
                    String.Format("clamav: {0}", handler.IsAliveClamAv ? "available" : "not available"),
                    String.Format("xslweb: {0}", handler.IsAliveXslWeb ? "available" : "not available"),
                    String.Format("droid: {0}", handler.IsAliveDroid ? "available" : "not available"),
                    String.Format("database: {0}", handler.IsAliveDatabase ? "available" : "not available")
                }
            });
        }

        [HttpPost("calculate/{guid}", Name = "Collection checksum calculation. Options : MD5, SHA1, SHA256, SHA512", Order = 1)]
        public IActionResult CollectionChecksumCalculation(Guid guid, [FromBody] BodyChecksum checksum)
        {
            _logger.LogInformation("Enter CollectionChecksumCalculation.");
            if(checksum == null)
                return Problem("Post Json body is null!");

            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            if (String.IsNullOrEmpty(checksum.ChecksumType))
                return BadRequest("Missing checksum type.");

            ContainerChecksumHandler handler = HttpContext.RequestServices.GetService(typeof(ContainerChecksumHandler)) as ContainerChecksumHandler;  
            if (handler == null)
                return Problem("Object is not loaded.", typeof(ContainerChecksumHandler).Name);

            handler.Logger = _logger;            
            handler.Checksum = checksum.ChecksumType;
            handler.DeliveredChecksumValue = checksum.InputChecksumValue;
            //database process id
            Guid processId = Guid.Empty;
            try
            {
                //data map id            
                handler.SetSessionGuid(guid);

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(ContainerChecksumHandler).Name, handler.SessionGuid);

                processId = handler.AddProcessAction(typeof(ContainerChecksumHandler).Name, String.Format("Container file {0}", handler.TarFilename), String.Concat(typeof(ContainerChecksumHandler).Name, ".json"));

                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });                 
            }
            catch (Exception e )
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(ContainerChecksumHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(ContainerChecksumHandler).Name);
            }

            _logger.LogInformation("Exit CollectionChecksumCalculation.");
            return new JsonResult(new { Message = String.Format("Container checksum calculation {0} is started", handler.TarFilename), SessionId = handler.SessionGuid, ActionId = processId  });
        }

        //Voorbereiding  
        [HttpPost("unpack/{guid}", Name = "Unpack tar collection", Order = 2)]
        public IActionResult Unpack(Guid guid)
        {
            _logger.LogInformation("Enter Unpack.");

            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            UnpackTarHandler handler = HttpContext.RequestServices.GetService(typeof(UnpackTarHandler)) as UnpackTarHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(UnpackTarHandler).Name);

            handler.Logger = _logger;
           
            //database action id
            Guid processId = Guid.Empty;
            try
            { 
                handler.SetSessionGuid(guid);
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(UnpackTarHandler).Name, handler.SessionGuid);               

                processId = handler.AddProcessAction(typeof(UnpackTarHandler).Name, String.Format("Container file {0}", handler.TarFilename), String.Concat(typeof(UnpackTarHandler).Name, ".json"));

                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch(Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(UnpackTarHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(UnpackTarHandler).Name);
            }

            _logger.LogInformation("Exit Unpack.");
            return new JsonResult(new { Message = String.Format ("Unpack tar container '{0}' started", handler.TarFilename), SessionId = handler.SessionGuid, ActionId = processId });
        }

        [HttpPost("virusscan/{guid}", Name = "Virusscan check", Order = 3)]
        public IActionResult VirusScan(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter VirusScan.");      

            ScanVirusValidationHandler handler = HttpContext.RequestServices.GetService(typeof(ScanVirusValidationHandler)) as ScanVirusValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(ScanVirusValidationHandler).Name);
            
            handler.Logger = _logger;            
            //database process id
            Guid processId = Guid.Empty;

            try
            {
                //data map id
                handler.SetSessionGuid(guid);
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(ScanVirusValidationHandler).Name, guid.ToString());

                processId = handler.AddProcessAction(typeof(ScanVirusValidationHandler).Name, String.Format("Scan for virus on folder {0}", guid), String.Concat(typeof(ScanVirusValidationHandler).Name, ".json"));

                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(ScanVirusValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(ScanVirusValidationHandler).Name);
            }

            _logger.LogInformation("Exit VirusScan.");
            return new JsonResult(new { Message = String.Format("Virusscan started."), SessionId = guid, ActionId = processId });
        }
                
          [HttpPost("naming/{guid}", Name = "Naming check", Order = 4)]
        public IActionResult Naming(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Naming.");          

            NamingValidationHandler handler = HttpContext.RequestServices.GetService(typeof(NamingValidationHandler)) as NamingValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(NamingValidationHandler).Name);

            handler.Logger = _logger;
                      
            //database process id
            Guid processId = Guid.Empty;
            try
            {
                //data map id
                handler.SetSessionGuid(guid);               

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(NamingValidationHandler).Name, guid.ToString());
               
                processId = handler.AddProcessAction(typeof(NamingValidationHandler).Name, String.Format("Name check on folders, sub-folders and files : folder {0}", guid), String.Concat(typeof(NamingValidationHandler).Name, ".json"));
                       
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(NamingValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(NamingValidationHandler).Name);
            }

            _logger.LogInformation("Exit Naming.");
            return new JsonResult(new { Message = String.Format("Folder(s) and file(s) naming check started."), SessionId = guid, ActionId = processId });
        }
       
        [HttpPost("sidecar/{guid}", Name = "Sidecar check", Order = 5)]
        public IActionResult Sidecar(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Sidecar.");          

            SidecarValidationHandler handler = HttpContext.RequestServices.GetService(typeof(SidecarValidationHandler)) as SidecarValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(SidecarValidationHandler).Name);

            handler.Logger = _logger;
            
            //database process id
            Guid processId = Guid.Empty;
            try
            {
                handler.SetSessionGuid(guid);

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(SidecarValidationHandler).Name, guid.ToString());
               
                //processId = handler.AddProcessAction("Sidecar", String.Format("Sidecar structure check for aggregation and metadata : folder {0}", guid), String.Concat(typeof(SidecarValidationHandler).Name, ".json", ";", typeof(SidecarValidationHandler).Name, ".bin"));
                processId = handler.AddProcessAction(typeof(SidecarValidationHandler).Name, String.Format("Sidecar structure check for aggregation and metadata : folder {0}", guid), String.Concat(typeof(SidecarValidationHandler).Name, ".json"));

                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(SidecarValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(SidecarValidationHandler).Name);
            }
            _logger.LogInformation("Exit Sidecar.");
            return new JsonResult(new { Message = String.Format("Structure sidecar check started."), SessionId = guid, ActionId = processId });
        }              
 
        [HttpPost("profiling/{guid}", Name = "Droid create profile", Order = 6)]
        public IActionResult Profiling(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Profiling.");

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

           
            string actionId = string.Empty;
            try
            {  
                handler.Logger = _logger;
                handler.SetSessionGuid(guid);                
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                
                var result = handler.GetProfiles().Result;
                _logger.LogInformation("Profiling is completed.");

                actionId = (result != null) ? result.ActionId : string.Empty;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit Profiling.");

            return new JsonResult(new { Message = String.Format("Droid profiling is started."), SessionId = handler.SessionGuid, ActionId = actionId });
        }

        [HttpPost("exporting/{guid}", Name = "Droid exporting result (CSV)", Order = 7)]
        public IActionResult Exporting(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Exporting.");

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

            string actionId = string.Empty;
            try
            {
            
                handler.Logger = _logger;
                handler.SetSessionGuid(guid);
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                
                var result = handler.GetExporting().Result;
                _logger.LogInformation("Exporting is completed.");
                actionId = (result != null) ? result.ActionId : string.Empty;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit Exporting.");

            return new JsonResult(new { Message = String.Format("Droid exporting (CSV) is started."), SessionId = handler.SessionGuid, ActionId = actionId });
        }       

        [HttpPost("reporting/{type}/{guid}", Name = "Droid reporting PDF/Droid (XML)/Planets (XML)", Order = 8)]
        public IActionResult Reporting(Guid guid, String type)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter Reporting.");

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

            
            string actionId = string.Empty;
            try
            {
                handler.Logger = _logger;
                handler.SetSessionGuid(guid);

                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                var result = handler.GetReporting(style).Result;
                _logger.LogInformation("Reporting is completed.");
                actionId = (result != null) ? result.ActionId : string.Empty;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit Reporting.");

            return new JsonResult(new { Message = String.Format("Droid reporting ({0}) is started.", style), SessionId = handler.SessionGuid, ActionId = actionId });
        }

        [HttpPut("signature/update", Name = "Droid signature update", Order = 9)]
        public IActionResult SignatureUpdate()
        {
            _logger.LogInformation("Enter SignatureUpdate.");

            DroidValidationHandler handler = HttpContext.RequestServices.GetService(typeof(DroidValidationHandler)) as DroidValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(DroidValidationHandler).Name);

            handler.Logger = _logger;                    
            try
            {
                _logger.LogInformation("Execute handler ({0}).", typeof(DroidValidationHandler).Name);
                var result = handler.SetSignatureUpdate().Result;               
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(DroidValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(DroidValidationHandler).Name);
            }

            _logger.LogInformation("Exit SignatureUpdate.");

            return Ok();
        }

        [HttpPost("greenlist/{guid}", Name = "Greenlist check", Order = 10)]
        public IActionResult Greenlist(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GreenListCheck.");                     

            GreenListHandler handler = HttpContext.RequestServices.GetService(typeof(GreenListHandler)) as GreenListHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(GreenListHandler).Name);

            handler.Logger = _logger;
                       
            //database process id
            Guid processId = Guid.Empty;

            try
            {
                handler.SetSessionGuid(guid);

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(GreenListHandler).Name, guid.ToString());
                
                processId = handler.AddProcessAction(typeof(GreenListHandler).Name, String.Format("Compare CSV result with greenlist"), String.Concat(typeof(GreenListHandler).Name, ".json"));
                               
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(GreenListHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(GreenListHandler).Name);
            }
            _logger.LogInformation("Exit GreenListCheck.");
            return new JsonResult(new { Message = String.Format("Greenlist check is started."), SessionId = guid, ActionId = processId });
        }

        [HttpPost("encoding/{guid}", Name = "Encoding check .metadata files", Order = 11)]
        public IActionResult Encoding(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter EncodingCheck.");

            EncodingHandler handler = HttpContext.RequestServices.GetService(typeof(EncodingHandler)) as EncodingHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(EncodingHandler).Name);

            handler.Logger = _logger;
            //database process id
            Guid processId = Guid.Empty;

            try
            {
                //data map id
                handler.SetSessionGuid(guid);
                
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(EncodingHandler).Name, guid.ToString());
                
                processId = handler.AddProcessAction(typeof(EncodingHandler).Name, String.Format("Retrieve the encoding for all metadata files : folder {0}", guid), String.Concat(typeof(EncodingHandler).Name, ".json"));
                                
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(EncodingHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(EncodingHandler).Name);
            }
            _logger.LogInformation("Exit EncodingCheck.");
            return new JsonResult(new { Message = String.Format("Encoding UTF-8 .metadata files check is started."), SessionId = guid, ActionId = processId });
        }

        [HttpPost("validate/{guid}", Name = "Validate .metadata files", Order = 12)]
        public IActionResult ValidateMetadata(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter ValidateMetadata.");           

            MetadataValidationHandler handler = HttpContext.RequestServices.GetService(typeof(MetadataValidationHandler)) as MetadataValidationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(MetadataValidationHandler).Name);

            handler.Logger = _logger;
            
            //database process id
            Guid processId = Guid.Empty;

            try
            {
                handler.SetSessionGuid(guid);
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(MetadataValidationHandler).Name, guid.ToString());
             
                processId = handler.AddProcessAction(typeof(MetadataValidationHandler).Name, String.Format("Validate all metadata files with XSD schema and schema+ : folder {0}", guid), String.Concat(typeof(MetadataValidationHandler).Name, ".json"));
                                
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(MetadataValidationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(MetadataValidationHandler).Name);
            }
            _logger.LogInformation("Exit ValidateMetadata.");
            return new JsonResult(new { Message = String.Format("Validate metadata files is started."), SessionId = guid, ActionId = processId });
        }

        [HttpPost("transform/{guid}", Name = "Transform .metadata files to .xip", Order = 13)]
        public IActionResult TransformXip(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter TransformXip.");         

            TransformationHandler handler = HttpContext.RequestServices.GetService(typeof(TransformationHandler)) as TransformationHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(TransformationHandler).Name);

            handler.Logger = _logger;
            //database process id
            Guid processId = Guid.Empty;

            try
            {
                handler.SetSessionGuid(guid);

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(TransformationHandler).Name, guid.ToString());                                

                processId = handler.AddProcessAction(typeof(TransformationHandler).Name, String.Format("Transform metadata files to XIP files : folder {0}", guid), String.Concat(typeof(TransformationHandler).Name, ".json"));
                               
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(TransformationHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(TransformationHandler).Name);
            }
            _logger.LogInformation("Exit TransformXip.");
            return new JsonResult(new { Message = String.Format("Transforming to XIP started."), SessionId = guid, ActionId = processId });
        }
        
        [HttpPost("sipcreator/{guid}", Name = "Start to create sip", Order = 14)]
        public IActionResult CreateSip(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter CreateSip.");
          
            SipCreatorHandler handler = HttpContext.RequestServices.GetService(typeof(SipCreatorHandler)) as SipCreatorHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(SipCreatorHandler).Name);

            handler.Logger = _logger;
            //database process id
            Guid processId = Guid.Empty;
            try
            {
                handler.SetSessionGuid(guid);
                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(SipCreatorHandler).Name, guid.ToString());
                //Task.Run(() => handler.Execute());
                handler.Execute();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(SipCreatorHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(SipCreatorHandler).Name);
            }

            _logger.LogInformation("Exit CreateSip.");
            return new JsonResult(new { Message = String.Format("Sip creator is started."), SessionId = guid, ActionId = processId });
        }

        [HttpPost("excelcreator/{guid}", Name = "Generate Excel report", Order = 15)]
        public IActionResult CreateExcel(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter CreateExcel.");

            ExcelCreatorHandler handler = HttpContext.RequestServices.GetService(typeof(ExcelCreatorHandler)) as ExcelCreatorHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(ExcelCreatorHandler).Name);

            handler.Logger = _logger;
            //database process id
            Guid processId = Guid.Empty;
            try
            {
                handler.SetSessionGuid(guid);

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(ExcelCreatorHandler).Name, guid.ToString());
                               
                //Should be called by XSLWeb service                
                processId = handler.AddProcessAction(typeof(ExcelCreatorHandler).Name, String.Format("Create Excel from folder {0}", guid), String.Concat(String.Concat(typeof(ExcelCreatorHandler).Name, ".xlsx"),";", String.Concat(typeof(ExcelCreatorHandler).Name, ".json")));
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(ExcelCreatorHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(ExcelCreatorHandler).Name);
            }

            _logger.LogInformation("Exit CreateExcel.");
            return new JsonResult(new { Message = String.Format("Excel creator is started."), SessionId = guid, ActionId = processId });
        }

        [HttpPut("settings/{guid}", Name = "Save preingest extra setting(s)", Order = 16)]
        public IActionResult PutSettings(Guid guid, [FromBody] BodySettings settings)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter PutSettings.");

            SettingsHandler handler = HttpContext.RequestServices.GetService(typeof(SettingsHandler)) as SettingsHandler;
            if (handler == null)
                return Problem("Object is not loaded.", typeof(SettingsHandler).Name);

            handler.Logger = _logger;
            //database process id
            Guid processId = Guid.Empty;
            try
            {
                handler.SetSessionGuid(guid);
                handler.CurrentSettings = settings;

                _logger.LogInformation("Execute handler ({0}) with GUID {1}.", typeof(SettingsHandler).Name, guid.ToString());

                //Should be called by XSLWeb service                
                processId = handler.AddProcessAction(typeof(SettingsHandler).Name, String.Format("Save user input setting(s) for folder {0}", guid), String.Concat(typeof(SettingsHandler).Name, ".json"));
                Task.Run(() =>
                {
                    handler.ActionProcessId = processId;
                    try
                    {
                        handler.PreingestEvents += Trigger;
                        handler.Execute();
                    }
                    finally
                    {
                        handler.PreingestEvents -= Trigger;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", typeof(SettingsHandler).Name, e.Message);
                return ValidationProblem(e.Message, typeof(SettingsHandler).Name);
            }

            _logger.LogInformation("Exit PutSettings.");
            return new JsonResult(new { Message = String.Format("Settings is stored."), SessionId = guid, ActionId = processId });
        }
    }
}
