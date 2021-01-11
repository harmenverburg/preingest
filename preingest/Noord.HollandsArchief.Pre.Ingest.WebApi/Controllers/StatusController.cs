using System;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Status;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;
        private AppSettings _settings = null;
        private readonly IHubContext<PreingestEventHub> _eventHub;

        public StatusController(ILogger<StatusController> logger, IOptions<AppSettings> settings, IHubContext<PreingestEventHub> eventHub)
        {
            _logger = logger;
            _settings = settings.Value;
            _eventHub = eventHub;
        }

        [HttpGet("action/{actionGuid}", Name = "Retrieve an action from a preingest session", Order = 0)]
        public IActionResult GetAction(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GetActions.");


            PreingestAction action = null;
            PreingestStatisticsSummary summary = null;
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    action = context.PreingestActionCollection.Find(actionGuid);
                    if (action != null && action.StatisticsSummary != null)
                        summary = JsonConvert.DeserializeObject<PreingestStatisticsSummary>(action.StatisticsSummary);
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

            if (summary == null)
                return new JsonResult(new { action.Creation, action.Description, SessionId = action.FolderSessionId, action.Name, ActionId = action.ProcessId, action.ResultFiles, action.ActionStatus });
            else
                return new JsonResult(new { action.Creation, action.Description, SessionId = action.FolderSessionId, action.Name, ActionId = action.ProcessId, action.ResultFiles, action.ActionStatus, Summary = summary });
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
                    var result = context.PreingestActionCollection.Where(item 
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
                    var actions = context.ActionStateCollection.Where(item => item.ProcessId == actionGuid)
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
                    var result = context.PreingestActionCollection.Where(item => item.FolderSessionId == folderSessionGuid)
                    .Join(context.ActionStateCollection,
                        session => session.ProcessId,
                        status => status.ProcessId,
                        (session, status)
                    => new { Session = session, Statuses = status })
                    .GroupJoin(context.ActionStateMessageCollection,
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
            var session = new PreingestAction
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
                    context.Add<PreingestAction>(session);
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                    return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
                }
                finally 
                {
                    _logger.LogInformation("Exit AddProcessAction.");
                }
            } 
            
            return new JsonResult(session);
        }

        [HttpPut("update/{actionGuid}", Name = "Update an action status and summary", Order = 5)]
        public IActionResult UpdateProcessAction(Guid actionGuid, [FromBody] ActionUpdateBody data)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");
            if (data == null)
                return Problem("Input data is required");
            if (String.IsNullOrEmpty(data.Result))
                return Problem("Result of the action (success/error/failed) is required");
            if (String.IsNullOrEmpty(data.Summary))
                return Problem("Summary (accepted/rejected/processed) is required");

            _logger.LogInformation("Enter UpdateProcessAction.");

            PreingestAction currentAction = null;
            using (var context = new PreIngestStatusContext())
            {
                try
                {
                    currentAction = context.Find<PreingestAction>(actionGuid);
                    if (currentAction != null)
                    {
                        currentAction.ActionStatus = data.Result;
                        currentAction.StatisticsSummary = data.Summary;
                    }
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                    return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
                }
                finally
                {
                    _logger.LogInformation("Exit UpdateProcessAction.");
                }
            }

            if (currentAction == null)
                return NotFound();

            return new JsonResult(currentAction);
        }

        [HttpPost("start/{actionGuid}", Name = "Add a start status", Order = 6)]
        public IActionResult AddStartState(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter AddStartState.");

            var result = AddState(actionGuid, "Started");

            _logger.LogInformation("Exit AddStartState.");

            return result;
        }

        [HttpPost("completed/{actionGuid}", Name = "Add a completed status", Order = 7)]
        public IActionResult AddCompletedState(Guid actionGuid)
        {
            if (actionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter AddCompletedState.");

            var result = AddState(actionGuid, "Completed");

            _logger.LogInformation("Exit AddCompletedState.");

            return result;
        }

        [HttpPost("failed/{actionGuid}", Name = "Add a failed status", Order = 8)]
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

        [HttpDelete("reset/{folderSessionGuid}", Name = "Clear data for a session folder", Order = 9)]
        public IActionResult ResetSession(Guid folderSessionGuid)
        {
            return DeleteSession(folderSessionGuid);
        }

        [HttpDelete("remove/{folderSessionGuid}", Name = "Remove session folder and clear the data for session folder", Order = 10)]
        public IActionResult RemoveSession(Guid folderSessionGuid)
        {
            return DeleteSession(folderSessionGuid, true);
        }

        [HttpPost("notify/{folderSessionGuid}", Name = "Notify the client about an event", Order = 11)]
        public IActionResult SendNotification(Guid folderSessionGuid, [FromBody] EventHubMessage message)
        {
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
                    EventDateTime = message.EventDateTime,
                    SessionId = message.SessionId,
                    Name = message.Name,
                    State = message.State,
                    Message = message.Message,
                    Summary = message.Summary
                }, settings)).GetAwaiter().GetResult();

            return Ok();
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
                var currentSession = context.Find<PreingestAction>(actionGuid);
                if (currentSession != null)
                {
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = statusValue, ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ActionStates>(item);
                    StateMessage stateMessage = null;
                    try
                    {
                        context.SaveChanges();

                        if (!String.IsNullOrEmpty(message))
                        {
                            stateMessage = new StateMessage
                            {
                                Creation = DateTime.Now,
                                Description = message,
                                MessageId = Guid.NewGuid(),
                                Status = item,
                                StatusId = item.StatusId
                            };
                            context.Add<StateMessage>(stateMessage);
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
        private IActionResult DeleteSession(Guid folderSessionGuid, bool deleteFolder = false)
        {
            if (folderSessionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter DeleteSession.");
 
            using (var context = new PreIngestStatusContext())
            {               
                try
                { 
                    if (deleteFolder)
                    {
                        System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(System.IO.Path.Combine(_settings.DataFolderName, folderSessionGuid.ToString()));
                        if (di.Exists)
                            di.Delete(true);
                    }
                    var sessions = context.PreingestActionCollection.Where(item => item.FolderSessionId == folderSessionGuid).ToList();
                    var statusus = context.ActionStateCollection.Where(item => sessions.Exists(exists => exists.ProcessId == item.ProcessId)).ToList();
                    var messages = context.ActionStateMessageCollection.Where(item => statusus.Exists(exists => exists.StatusId == item.MessageId)).ToList();

                    context.RemoveRange(messages);
                    context.RemoveRange(statusus);
                    context.RemoveRange(sessions);

                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace);
                    return ValidationProblem(String.Format("An exception is throwned : {0}, '{1}'.", e.Message, e.StackTrace));
                }
                finally { }
            }

            _logger.LogInformation("Exit DeleteSession.");

            return Ok();
        }
    }
}
