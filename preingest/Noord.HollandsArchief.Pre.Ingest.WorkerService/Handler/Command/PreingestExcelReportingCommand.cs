using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class PreingestExcelReportingCommand : AbstractPreingestCommand
    {
        public override ValidationActionType ActionTypeName => ValidationActionType.ExcelCreatorHandler;
        public PreingestExcelReportingCommand(ILogger<PreingestEventHubHandler> logger, Uri webapi) : base(logger, webapi) { }

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
                api.ExcelcreatorAsync(id).GetAwaiter().GetResult();
            });
        }
    }
}

