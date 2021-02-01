using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class CalculateChecksumCommand : AbstractPreingestCommand
    {
        public override ValidationActionType ActionTypeName => ValidationActionType.ContainerChecksumHandler;

        public CalculateChecksumCommand(ILogger<PreingestEventHubHandler> logger, Uri webapi) : base(logger, webapi) { }

        public override void Execute(HttpClient client, Guid currentFolderSessionId, Settings settings)
        {
            TryExecuteOrCatch(client, currentFolderSessionId, settings, (id, props) =>
            {                
                Logger.LogInformation("Command: {0}", this.GetType().Name);
                OpenAPIService.PreingestClient api = new OpenAPIService.PreingestClient(WebApi.ToString(), client);

                if (props == null)
                {            
                    string message = "Settings is empty! Object reference is null. Please save the settings first before starting the run.";                 
                    throw new ApplicationException(message);
                }

                api.CalculateAsync(id, new OpenAPIService.BodyChecksum
                {
                    ChecksumType = props.ChecksumType,
                    InputChecksumValue = props.ChecksumValue
                }).GetAwaiter().GetResult();                
            });
        }

        public override void Execute(HttpClient client, Guid currentFolderSessionId)
        {
            Execute(client, currentFolderSessionId, null);
        }
    }
}
