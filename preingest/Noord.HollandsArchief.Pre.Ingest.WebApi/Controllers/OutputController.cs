using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;

using Newtonsoft.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutputController : ControllerBase
    {
        private readonly ILogger<OutputController> _logger;
        private readonly AppSettings _settings = null;
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

            dynamic dataResults = null;
            List<JoinedQueryResult> currentActions = new List<JoinedQueryResult>();
            List<ExecutionPlan> executionPlan = new List<ExecutionPlan>();
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    var query = context.PreingestActionCollection.Where(item => tarArchives.Select(tar
                        => ChecksumHelper.GeneratePreingestGuid(tar.Name)).ToList().Contains(item.FolderSessionId)).Join(context.ActionStateCollection,
                        states => states.ProcessId,
                        actions => actions.ProcessId,
                        (actions, states)
                        => new JoinedQueryResult { Actions = actions, States = states }).ToList();

                    if (query != null && query.Count > 0)
                        currentActions.AddRange(query);

                    var plans = context.ExecutionPlanCollection.Where(item => tarArchives.Select(tar
                       => ChecksumHelper.GeneratePreingestGuid(tar.Name)).ToList().Contains(item.SessionId)).ToArray();

                    if (plans != null && plans.Length > 0)
                        executionPlan.AddRange(plans);
                }

                if (currentActions != null || currentActions.Count > 0)
                {
                    var joinedActions = currentActions.Select(actions => actions.Actions).Distinct().Select(item => new QueryResultAction
                    {
                        ActionStatus = String.IsNullOrEmpty(item.ActionStatus) ? "Executing" : item.ActionStatus,
                        Creation = item.Creation,
                        Description = item.Description,
                        FolderSessionId = item.FolderSessionId,
                        Name = item.Name,
                        ProcessId = item.ProcessId,
                        ResultFiles = item.ResultFiles.Split(";").ToArray(),
                        Summary = String.IsNullOrEmpty(item.StatisticsSummary) ? null : JsonConvert.DeserializeObject<PreingestStatisticsSummary>(item.StatisticsSummary),
                        States = currentActions.Select(state
                            => state.States).Where(state
                                => state.ProcessId == item.ProcessId).Select(state
                                    => new QueryResultState
                                    {
                                        StatusId = state.StatusId,
                                        Name = state.Name,
                                        Creation = state.Creation
                                    }).ToArray()
                    });

                    dataResults = tarArchives.OrderByDescending(item
                    => item.CreationTime).Select(item
                        => new
                        {
                            Name = item.Name,
                            SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name),
                            CreationTime = item.CreationTime,
                            LastWriteTime = item.LastWriteTime,
                            LastAccessTime = item.LastAccessTime,
                            Size = item.Length,                            
                            Settings = new SettingsReader(item.DirectoryName, ChecksumHelper.GeneratePreingestGuid(item.Name)).GetSettings(),
                            ScheduledPlan = new ScheduledPlanStatusHandler(executionPlan, joinedActions.Where(preingestActions => preingestActions.FolderSessionId == ChecksumHelper.GeneratePreingestGuid(item.Name))),
                            OverallStatus = new ContainerOverallStatusHandler(joinedActions.Where(preingestActions
                                => preingestActions.FolderSessionId == ChecksumHelper.GeneratePreingestGuid(item.Name))).GetContainerStatus(),
                            Preingest = joinedActions.Where(preingestActions
                                => preingestActions.FolderSessionId == ChecksumHelper.GeneratePreingestGuid(item.Name)).ToArray()
                        }).ToArray();
                }
                else
                {
                    dataResults = tarArchives.OrderByDescending(item
                    => item.CreationTime).Select(item
                        => new
                        {
                            Name = item.Name,
                            SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name),
                            CreationTime = item.CreationTime,
                            LastWriteTime = item.LastWriteTime,
                            LastAccessTime = item.LastAccessTime,
                            Size = item.Length,
                            OverallStatus = ContainerStatus.New,
                            Preingest = new object[] { }
                        }).ToArray();
                }
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

            var tarArchives = directory.GetFiles("*.*").Where(s => s.Extension.EndsWith(".tar") || s.Extension.EndsWith(".gz"));
            bool exists = tarArchives.Select(tar => ChecksumHelper.GeneratePreingestGuid(tar.Name)).Contains(guid);
            if (!exists)
                return Problem(String.Format ("No tar container file found with GUID {0}!", guid));

            dynamic  dataResults = null;
            List<JoinedQueryResult> currentActions = new List<JoinedQueryResult>();
            List<ExecutionPlan> executionPlan = new List<ExecutionPlan>();

            try
            {
                
                using (var context = new PreIngestStatusContext())
                {
                    //compare the tar list with db context if a guid exists in both collection AND then filter Guid from IN parameter
                    var query = context.PreingestActionCollection.Where(item => item.FolderSessionId == guid).Join(context.ActionStateCollection,
                        states => states.ProcessId,
                        actions => actions.ProcessId,
                        (actions, states)
                        => new JoinedQueryResult { Actions = actions, States = states }).ToList();

                    if (query != null && query.Count > 0)
                        currentActions.AddRange(query);

                    var plans = context.ExecutionPlanCollection.Where(item => item.SessionId == guid).ToArray();

                    if (plans != null && plans.Length > 0)
                        executionPlan.AddRange(plans);
                }

                if (currentActions != null || currentActions.Count > 0)
                {
                    var joinedActions = currentActions.Select(actions => actions.Actions).Distinct().Select(item => new QueryResultAction
                    {
                        ActionStatus = String.IsNullOrEmpty(item.ActionStatus) ? "Executing" : item.ActionStatus,
                        Creation = item.Creation,
                        Description = item.Description,
                        FolderSessionId = item.FolderSessionId,
                        Name = item.Name,
                        ProcessId = item.ProcessId,
                        ResultFiles = item.ResultFiles.Split(";").ToArray(),
                        Summary = String.IsNullOrEmpty(item.StatisticsSummary) ? null : JsonConvert.DeserializeObject<PreingestStatisticsSummary>(item.StatisticsSummary),
                        States = currentActions.Select(state
                            => state.States).Where(state
                                => state.ProcessId == item.ProcessId).Select(state
                                    => new QueryResultState
                                    {
                                        StatusId = state.StatusId,
                                        Name = state.Name,
                                        Creation = state.Creation
                                    }).ToArray()
                    });

                    dataResults = tarArchives.OrderByDescending(item
                    => item.CreationTime).Select(item
                        => new
                        {
                            Name = item.Name,
                            SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name),
                            CreationTime = item.CreationTime,
                            LastWriteTime = item.LastWriteTime,
                            LastAccessTime = item.LastAccessTime,
                            Size = item.Length,
                            Settings = new SettingsReader(item.DirectoryName, ChecksumHelper.GeneratePreingestGuid(item.Name)).GetSettings(),
                            ScheduledPlan = new ScheduledPlanStatusHandler(executionPlan, joinedActions.Where(preingestActions => preingestActions.FolderSessionId == ChecksumHelper.GeneratePreingestGuid(item.Name))),
                            OverallStatus = new ContainerOverallStatusHandler(joinedActions.Where(preingestActions => preingestActions.FolderSessionId == ChecksumHelper.GeneratePreingestGuid(item.Name))).GetContainerStatus(),
                            Preingest = joinedActions.Where(preingestActions => preingestActions.FolderSessionId == ChecksumHelper.GeneratePreingestGuid(item.Name)).ToArray()
                        }).FirstOrDefault(item => item.SessionId == guid);
                }
                else
                {
                    dataResults = tarArchives.OrderByDescending(item
                    => item.CreationTime).Select(item
                        => new
                        {
                            Name = item.Name,
                            SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name),
                            CreationTime = item.CreationTime,
                            LastWriteTime = item.LastWriteTime,
                            LastAccessTime = item.LastAccessTime,
                            Size = item.Length,
                            OverallStatus = ContainerStatus.New,
                            Preingest = new object[] { }
                        }).FirstOrDefault(item => item.SessionId == guid);
                }
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
