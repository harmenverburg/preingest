using Microsoft.Extensions.Logging;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class SipZipCopyCommand : AbstractPreingestCommand
    {
        public override ValidationActionType ActionTypeName => ValidationActionType.SipZipCopyHandler;

        public SipZipCopyCommand(ILogger<PreingestEventHubHandler> logger, Uri webapi) : base(logger, webapi) { }

        public override void Execute(HttpClient client, Guid currentFolderSessionId)
        {
            Execute(client, currentFolderSessionId, null);
        }

        public override void Execute(HttpClient client, Guid currentFolderSessionId, Settings settings)
        {
            TryExecuteOrCatch(client, currentFolderSessionId, (id) =>
            {
                Logger.LogInformation("Command: {0}", this.GetType().Name);
                OpenAPIService.SipzipClient api = new OpenAPIService.SipzipClient(WebApi.ToString(), client);
                api.TransferAsync(id).GetAwaiter().GetResult();
            });
        }
    }
}
