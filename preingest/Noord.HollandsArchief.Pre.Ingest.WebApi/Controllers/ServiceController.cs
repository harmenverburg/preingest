using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Service;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;

using System;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly ILogger<ServiceController> _logger;
        private readonly AppSettings _settings = null;
        private readonly IHubContext<PreingestEventHub> _eventHub;
        private readonly CollectionHandler _preingestCollection = null;

        public ServiceController(ILogger<ServiceController> logger, IOptions<AppSettings> settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection)
        {
            _logger = logger;
            _settings = settings.Value;
            _eventHub = eventHub;
            _preingestCollection = preingestCollection;
        }

        [HttpPost("startplan/{guid}", Name = "Auto run preingest by worker service", Order = 1)]
        public IActionResult StartPlan(Guid guid, [FromBody] BodyExecutionPlan workflow)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            if(workflow == null)
                return Problem("Empty execution plan is invalid.");

            _logger.LogInformation("Enter StartPlan.");      
            try
            {
                var currentArchive = _preingestCollection.GetCollection(guid);

                if (currentArchive.ScheduledPlan == null) {
                    //save scheduled/execution plan in db
                    using (var context = new PreIngestStatusContext())
                    {
                        var plan = workflow.Workflow.Select(item => new ExecutionPlan
                        {
                            ActionName = item.ActionName.ToString(),
                            SessionId = guid,
                            ContinueOnError = item.ContinueOnError,
                            ContinueOnFailed = item.ContinueOnFailed
                        });

                        context.ExecutionPlanCollection.AddRange(plan);

                        try
                        {
                            context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            return Problem("Failed to save the plan! " + e.Message);
                        }
                    }
                    //reload
                    currentArchive = _preingestCollection.GetCollection(guid);
                }

                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string collectionData = JsonConvert.SerializeObject(currentArchive, settings);
                _eventHub.Clients.All.SendAsync(nameof(IEventHub.CollectionStatus), guid, collectionData).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.LogInformation("Exit StartPlan.");
                return ValidationProblem(e.Message);
            }

            _logger.LogInformation("Exit StartPlan.");
            return Ok();
        }

        [HttpDelete("cancelplan/{guid}", Name = "Delete an autorun preingest in worker service by GUID", Order = 2)]
        public IActionResult CancelPlan(Guid guid)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            _logger.LogInformation("Enter CancelPlan.");
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    var items = context.ExecutionPlanCollection.Where(item => item.SessionId == guid).ToArray();

                    if (items != null && items.Length > 0)
                        context.ExecutionPlanCollection.RemoveRange(items);

                    try
                    {
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        return Problem("Failed to remove the plan! " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation("Exit CancelPlan.");
                return ValidationProblem(e.Message);
            }

            _logger.LogInformation("Exit CancelPlan.");
            return Ok();
        }
    }
}
