using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class CalculateChecksumCommand : AbstractPreingestCommand
    {
        public CalculateChecksumCommand(ILogger<PreingestEventHubHandler> logger, Uri webapi) : base(logger, webapi) { }
        public override void Execute(HttpClient client)
        {
           TryExecuteOrCatch(() =>
           {
               Logger.LogInformation("Command: {0} .", this.GetType().Name);
               OpenAPIService.Client api = new OpenAPIService.Client(WebApi.ToString(), client);
               api.ApiPreingestCalculateAsync(this.CurrentSessionId, new OpenAPIService.BodyChecksum
               {
                   ChecksumType = CurrentSettings.ChecksumType,
                   InputChecksumValue = CurrentSettings.ChecksumValue
               }).GetAwaiter().GetResult();
           });
        }
    }
}
