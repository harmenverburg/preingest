using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Status;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;
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
        private readonly CollectionHandler _preingestCollection = null;

        public StatusController(ILogger<StatusController> logger, IOptions<AppSettings> settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection)
        {
            _logger = logger;
            _settings = settings.Value;
            _eventHub = eventHub;
            _preingestCollection = preingestCollection;
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
                _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }

            _logger.LogInformation("Exit GetActions.");

            if (action == null)
                return NotFound(String.Format("Action not found with ID '{0}'", actionGuid));

            if (summary == null)
                return new JsonResult(new { action.Creation, action.Description, SessionId = action.FolderSessionId, action.Name, ActionId = action.ProcessId, ResultFiles = action.ResultFiles.Split(";").ToArray(), action.ActionStatus });
            else
                return new JsonResult(new { action.Creation, action.Description, SessionId = action.FolderSessionId, action.Name, ActionId = action.ProcessId, ResultFiles = action.ResultFiles.Split(";").ToArray(), action.ActionStatus, Summary = summary });
        }

        [HttpGet("actions/{folderSessionGuid}", Name = "Retrieve all actions from a preingest session", Order = 1)]
        public IActionResult GetActions(Guid folderSessionGuid)
        {
            if (folderSessionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter GetActions.");

            List<PreingestAction> actions = new List<PreingestAction>();

            dynamic jsonResult = null;

            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    var result = context.PreingestActionCollection.Where(item => item.FolderSessionId == folderSessionGuid).ToList();
                    if (result != null)
                        actions.AddRange(result);                 
                }

                jsonResult = actions.Select(action => new
                {
                    action.Creation,
                    action.Description,
                    SessionId = action.FolderSessionId,
                    action.Name,
                    ActionId = action.ProcessId,
                    action.ResultFiles,
                    action.ActionStatus,
                    Summary = String.IsNullOrEmpty(action.StatisticsSummary) ? new object { } : JsonConvert.DeserializeObject<PreingestStatisticsSummary>(action.StatisticsSummary)
                }).ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception was thrown : {0}, '{1}'.",  e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }

            _logger.LogInformation("Exit GetActions.");


            if (jsonResult == null)
                return new JsonResult(new object[] { });

            return new JsonResult(jsonResult);            
        }
         
        [HttpPost("new/{folderSessionGuid}", Name = "Add an action", Order = 3)]
        public IActionResult AddProcessAction(Guid folderSessionGuid, [FromBody] BodyNewAction data)
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
                Creation = DateTimeOffset.Now,
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
                    _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                    return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
                }
                finally 
                {
                    _logger.LogInformation("Exit AddProcessAction.");
                }
            } 
            
            return new JsonResult(session);
        }

        [HttpPut("update/{actionGuid}", Name = "Update an action status and summary", Order = 4)]
        public IActionResult UpdateProcessAction(Guid actionGuid, [FromBody] BodyUpdate data)
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
                    _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                    return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
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
        public IActionResult AddFailedState(Guid actionGuid, [FromBody] BodyMessage failMessage)
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

        [HttpDelete("reset/{folderSessionGuid}", Name = "Clear data for a session folder", Order = 8)]
        public IActionResult ResetSession(Guid folderSessionGuid)
        {
            return DeleteSession(folderSessionGuid);
        }

        [HttpDelete("remove/{folderSessionGuid}", Name = "Remove session folder and clear the data for session folder", Order = 9)]
        public IActionResult RemoveSession(Guid folderSessionGuid)
        {
            return DeleteSession(folderSessionGuid, true);
        }

        [HttpPost("notify", Name = "Notify the client about an event", Order = 11)]
        public IActionResult SendNotification([FromBody] BodyEventMessageBody message)
        {
            if (message == null)
                return Problem("POST body JSON object is null!");

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()

                },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            object state = null;
            bool parse = Enum.TryParse(typeof(PreingestActionStates), message.State, out state);
            if (!parse)
                return Problem("Parsing state failed!");

            //trigger full events
            _eventHub.Clients.All.SendAsync(nameof(IEventHub.SendNoticeEventToClient),
                JsonConvert.SerializeObject(new EventHubMessage
                {
                    EventDateTime = message.EventDateTime,
                    SessionId = message.SessionId,
                    Name = message.Name,
                    State = (PreingestActionStates)state,
                    Message = message.Message,
                    Summary = message.HasSummary ? new PreingestStatisticsSummary { Accepted = message.Accepted, Processed = message.Processed, Rejected = message.Rejected, Start = message.Start.Value, End = message.End.Value } : null
                }, settings)).GetAwaiter().GetResult();            

            if ((PreingestActionStates)state == PreingestActionStates.Failed || (PreingestActionStates)state == PreingestActionStates.Completed)
            {
                string collectionData = JsonConvert.SerializeObject(_preingestCollection.GetCollection(message.SessionId), settings);
                _eventHub.Clients.All.SendAsync(nameof(IEventHub.CollectionStatus), message.SessionId, collectionData).GetAwaiter().GetResult();
            }
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
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTimeOffset.Now, Name = statusValue, ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ActionStates>(item);
                    StateMessage stateMessage = null;
                    try
                    {
                        context.SaveChanges();

                        if (!String.IsNullOrEmpty(message))
                        {
                            stateMessage = new StateMessage
                            {
                                Creation = DateTimeOffset.Now,
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
                            catch (Exception e) { _logger.LogError(e, "An exception was thrown in {0}: '{1}'.", e.Message, e.StackTrace); }
                            finally { }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                        return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
                    }
                    finally { }

                    if (stateMessage != null)
                        result = new JsonResult(new { item.StatusId, item.ProcessId, item.Creation, item.Name, Message = new { stateMessage.Creation, stateMessage.Description, stateMessage.MessageId, stateMessage.StatusId } });
                    else
                        result = new JsonResult(new { item.StatusId, item.ProcessId, item.Creation, item.Name });              
                }
            }

            _logger.LogInformation("Exit AddState.");

            if (result == null)
                return NoContent();

            return result;
        }
        private IActionResult DeleteSession(Guid folderSessionGuid, bool fullDelete = false)
        {
            if (folderSessionGuid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter DeleteSession.");

            String containerLocation = String.Empty;
            if (fullDelete)
            {
                var tarArchive = Directory.GetFiles(_settings.DataFolderName, "*.*").Select(i => new FileInfo(i)).Where(s
                    => s.Extension.EndsWith(".tar") || s.Extension.EndsWith(".gz")).Select(item
                    => new
                    {
                        CollectionName = item.Name,
                        SessionFolderId = ChecksumHelper.GeneratePreingestGuid(item.Name),
                        CollectionFullName = item.FullName
                    }).Where(item => item.SessionFolderId == folderSessionGuid).FirstOrDefault();

                if (tarArchive == null)
                    return Problem(String.Format("Container not found with GUID {0}", folderSessionGuid));

                containerLocation = tarArchive.CollectionFullName;
            }

            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    var sessions = context.PreingestActionCollection.Where(item => item.FolderSessionId == folderSessionGuid).ToList();
                    var statusesIds = sessions.Select(item => item.ProcessId).ToArray();

                    var statusus = context.ActionStateCollection.Where(item => statusesIds.Contains(item.ProcessId)).ToList();
                    var messagesIds = statusus.Select(item => item.StatusId).ToArray();

                    var messages = context.ActionStateMessageCollection.Where(item => messagesIds.Contains(item.StatusId)).ToList();
                    
                    var scheduledPlan = context.ExecutionPlanCollection.Where(item => item.SessionId == folderSessionGuid).ToArray();

                    //remove any exeception messages
                    context.ActionStateMessageCollection.RemoveRange(messages);
                    //remove (actions) statusus
                    context.ActionStateCollection.RemoveRange(statusus);
                    //remove (folder session) actions
                    context.PreingestActionCollection.RemoveRange(sessions);
                    //remove plan                    
                    context.ExecutionPlanCollection.RemoveRange(scheduledPlan);

                    context.SavedChanges += (object sender, Microsoft.EntityFrameworkCore.SavedChangesEventArgs e) =>
                    {
                        try
                        {
                            DirectoryInfo di = new DirectoryInfo(Path.Combine(_settings.DataFolderName, folderSessionGuid.ToString()));
                            if (di.Exists)
                                di.Delete(true);
                        }
                        finally { }

                        if (fullDelete)
                        {
                            try
                            {
                                if (System.IO.File.Exists(containerLocation))
                                    System.IO.File.Delete(containerLocation);
                            }
                            finally { }
                        }
                    };
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace);
                return ValidationProblem(String.Format("An exception was thrown : {0}, '{1}'.", e.Message, e.StackTrace));
            }
            finally { }            

            _logger.LogInformation("Exit DeleteSession.");

            return Ok();
        }
    }
}
