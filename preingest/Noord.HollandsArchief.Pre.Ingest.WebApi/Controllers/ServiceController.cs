using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Service;

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly ILogger<ServiceController> _logger;
        private AppSettings _settings = null;
        private readonly IHubContext<PreingestEventHub> _eventHub;

        public ServiceController(ILogger<ServiceController> logger, IOptions<AppSettings> settings, IHubContext<PreingestEventHub> eventHub)
        {
            _logger = logger;
            _settings = settings.Value;
            _eventHub = eventHub;
        }

        [HttpPut("autorun/{guid}", Name = "Auto run preingest by worker service", Order = 1)]
        public IActionResult AutoRun(Guid guid, [FromBody] BodyExecutionPlan workflow)
        {
            if (guid == Guid.Empty)
                return Problem("Empty GUID is invalid.");

            if(workflow == null)
                return Problem("Empty execution plan is invalid.");

            _logger.LogInformation("Enter AutoRun.");      
            try
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var preingestSettings = new SettingsReader(_settings.DataFolderName, guid);
                if (preingestSettings == null)
                    throw new ApplicationException("The preingest settings file 'SettingsHandler.json' is not set or found! Please save the settings before running the worker service.");
                
                String jsonMessage = JsonConvert.SerializeObject(new { Settings = preingestSettings, Workflow = workflow }, jsonSettings);
                _eventHub.Clients.All.SendAsync(nameof(IEventHub.PushInQueue), jsonMessage, jsonSettings).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.LogInformation("Exit AutoRun.");
                return ValidationProblem(e.Message);
            }

            _logger.LogInformation("Exit AutoRun.");
            return Ok();
        }
    }
}
