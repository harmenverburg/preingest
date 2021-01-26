using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class DroidCsvExportingCommand : AbstractPreingestCommand
    {
        public DroidCsvExportingCommand(ILogger<PreingestEventHubHandler> logger, Uri webapi) : base(logger, webapi) { }
        public override void Execute(HttpClient client)
        {
            TryExecuteOrCatch(() =>
            {
                Logger.LogInformation("Command: {0} .", this.GetType().Name);
                OpenAPIService.Client api = new OpenAPIService.Client(WebApi.ToString(), client);
                api.ApiPreingestExportingAsync(this.CurrentSessionId).GetAwaiter().GetResult();
            });
        }
    }
}


