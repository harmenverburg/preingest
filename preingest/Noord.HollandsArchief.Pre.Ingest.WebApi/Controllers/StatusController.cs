using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        public class ActionFormBody
        {
            public String Name { get; set; }
            public String Description { get; set; }
            public String Result { get; set; }
        }

        public class ActionMessageBody
        {
            public String Message { get; set; }
        }

        private readonly ILogger<StatusController> _logger;
        private AppSettings _settings = null;

        public StatusController(ILogger<StatusController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpGet("action/{actionGuid}", Name = "Retrieve an action from a preingest session", Order = 0)]
        public IActionResult GetAction(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GetActions.");


            PreIngestSession action = null;
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    action = context.Sessions.Find(actionGuid);                   
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }

            _logger.LogInformation("Exit GetActions.");

            if (action == null)
                return NotFound(String.Format("Action not found with ID '{0}'", actionGuid));

            return new JsonResult(new { action.Creation, action.Description, action.FolderSessionId, action.Name, action.ProcessId, action.ResultFiles });
        }

        [HttpGet("actions/{folderSessionGuid}", Name = "Retrieve all actions from a preingest session", Order = 1)]
        public IActionResult GetActions(Guid folderSessionGuid)
        {
            if (folderSessionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GetActions.");

            JsonResult returnResult = null;
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    var result = context.Sessions.Where(item 
                        => item.FolderSessionId == folderSessionGuid).Select(item 
                        => new { item.ProcessId, item.Creation, item.Description, item.Name, item.FolderSessionId, item.ResultFiles }).ToList();

                    if (result == null || result.Count == 0)
                        returnResult = new JsonResult(new { Message = String.Format("Geen actie(s) gevonden voor map {0}.", folderSessionGuid) });

                    returnResult = new JsonResult(result);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned : {0}, '{1}'.",  e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }

            _logger.LogInformation("Exit GetActions.");

            return returnResult;
        }

        [HttpGet("result/{actionGuid}", Name = "Retrieve all status for an action", Order = 2)]
        public IActionResult GetStatus(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GetStatus.");

            JsonResult returnResult = null;
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    var actions = context.Statuses.Where(item => item.ProcessId == actionGuid)
                        .Select(status => new { status.Creation, status.Name, status.ProcessId, status.StatusId }).ToList();

                    if (actions == null || actions.Count == 0)
                        returnResult = new JsonResult(new { Message = String.Format("Geen resultaten gevonden voor actie {0}.", actionGuid) });

                    returnResult = new JsonResult(actions);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }

            _logger.LogInformation("Exit GetStatus.");

            return returnResult;
        }

        [HttpGet("complete/{folderSessionGuid}", Name = "Retrieve all information for a session", Order = 3)]
        public IActionResult GetFullStatus(Guid folderSessionGuid)
        {
            if (folderSessionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GetFullStatus.");

            JsonResult returnResult = null;
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    //inner join and left outer join
                    var result = context.Sessions.Where(item => item.FolderSessionId == folderSessionGuid)
                    .Join(context.Statuses,
                        session => session.ProcessId,
                        status => status.ProcessId,
                        (session, status)
                    => new { Session = session, Statuses = status })
                    .GroupJoin(context.Messages,
                        join => join.Statuses.StatusId,
                        message => message.StatusId,
                        (join, message) => new { join.Session, join.Statuses, Messages = message })
                    .SelectMany(x => x.Messages.DefaultIfEmpty(),
                                   (x, y) => new
                                   {
                                       Session = new { x.Session.ProcessId, x.Session.Creation, x.Session.Name, x.Session.Description, x.Session.FolderSessionId, x.Session.ResultFiles },
                                       Status = new { x.Statuses.StatusId, x.Statuses.Creation, x.Statuses.Name, x.Statuses.ProcessId, },
                                       Message = y != null ? new { y.MessageId, y.Creation, y.Description, y.StatusId } : null
                                   }).ToList();

                    if (result == null || result.Count == 0)
                        returnResult = new JsonResult(new { Message = String.Format("Geen resultaten gevonden voor map {0}.", folderSessionGuid) });


                    returnResult = new JsonResult(result);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }

            _logger.LogInformation("Exit GetFullStatus.");

            return returnResult;
        }

        [HttpPost("new/{folderSessionGuid}", Name = "Add an action", Order = 4)]
        public IActionResult AddProcessAction(Guid folderSessionGuid, [FromBody] ActionFormBody data)
        {
            if (folderSessionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");
            if(data == null)
                return Problem("Input data is required");
            if (String.IsNullOrEmpty(data.Name))
                return Problem("Name is required");
            if (String.IsNullOrEmpty(data.Description))
                return Problem("Description is required");
            if(String.IsNullOrEmpty(data.Result))
                return Problem("Result filename is required.");
            
            _logger.LogInformation("Enter AddProcessAction.");

            var processId = Guid.NewGuid();
            var session = new PreIngestSession
            {
                ProcessId = processId,
                FolderSessionId = folderSessionGuid,
                Description = data.Description,
                Name = data.Name,
                Creation = DateTime.Now,
                ResultFiles = data.Result
            };

            using (var context = new PreIngestStatusContext())
            {                
                try
                {
                    context.Add<PreIngestSession>(session);
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                    return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
                }
                finally { }
            }             

            _logger.LogInformation("Exit AddProcessAction.");

            return new JsonResult(session);
        }

        [HttpPost("start/{actionGuid}", Name = "Add a start status", Order = 5)]
        public IActionResult AddStartState(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter AddStartState.");

            var result = AddState(actionGuid, "Started");

            _logger.LogInformation("Exit AddStartState.");

            return result;
        }

        [HttpPost("completed/{actionGuid}", Name = "Add a completed status", Order = 6)]
        public IActionResult AddCompletedState(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter AddCompletedState.");

            var result = AddState(actionGuid, "Completed");

            _logger.LogInformation("Exit AddCompletedState.");

            return result;
        }

        [HttpPost("failed/{actionGuid}", Name = "Add a failed status", Order = 7)]
        public IActionResult AddFailedState(Guid actionGuid, [FromBody] ActionMessageBody failMessage)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter AddFailedState.");

            String message = String.Empty;
            if (failMessage != null)
                message = failMessage.Message;

            var result = AddState(actionGuid, "Failed", message);
                        
            _logger.LogInformation("Exit AddFailedState.");

            return result;
        }

        private IActionResult AddState(Guid actionGuid, String statusValue, String message = null)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            if(String.IsNullOrEmpty(statusValue))
                return Problem("Status value is required.");

            _logger.LogInformation("Enter AddState.");

            JsonResult result = null;
            using (var context = new PreIngestStatusContext())
            {
                var currentSession = context.Find<PreIngestSession>(actionGuid);
                if (currentSession != null)
                {
                    var item = new ProcessStatusItem { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = statusValue, ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ProcessStatusItem>(item);
                    StatusMessageItem stateMessage = null;
                    try
                    {
                        context.SaveChanges();

                        if (!String.IsNullOrEmpty(message))
                        {
                            stateMessage = new StatusMessageItem
                            {
                                Creation = DateTime.Now,
                                Description = message,
                                MessageId = Guid.NewGuid(),
                                Status = item,
                                StatusId = item.StatusId
                            };
                            context.Add<StatusMessageItem>(stateMessage);
                            try
                            {
                                context.SaveChanges();
                            }
                            catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                            finally { }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                        return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
                    }
                    finally { }

                    if (stateMessage != null)
                        result = new JsonResult(new {  item.StatusId, item.ProcessId, item.Creation, item.Name, Message = new { stateMessage.Creation, stateMessage.Description, stateMessage.MessageId, stateMessage.StatusId } });
                    else
                        result = new JsonResult(new { item.StatusId, item.ProcessId, item.Creation, item.Name });                   
                }
            }

            _logger.LogInformation("Exit AddState.");

            if (result == null)
                return NoContent();

            return result;
        }
    }
}
