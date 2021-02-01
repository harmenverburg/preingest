using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class DroidPlanetsReportingCommand : AbstractPreingestCommand
    {
        public override ValidationActionType ActionTypeName => ValidationActionType.ReportingPlanetsXmlHandler;

        public DroidPlanetsReportingCommand(ILogger<PreingestEventHubHandler> logger, Uri webapi) : base(logger, webapi) { }

        public override void Execute(HttpClient client, Guid currentFolderSessionId)
        {
            Execute(client, currentFolderSessionId, null);
        }

        public override void Execute(HttpClient client, Guid currentFolderSessionId, Settings settings)
        {
            TryExecuteOrCatch(client, currentFolderSessionId, (id) =>
            {
                Logger.LogInformation("Command: {0}", this.GetType().Name);
                OpenAPIService.PreingestClient api = new OpenAPIService.PreingestClient(WebApi.ToString(), client);
                api.ReportingAsync(id, "planets").GetAwaiter().GetResult();
            });
        }
    }
}

