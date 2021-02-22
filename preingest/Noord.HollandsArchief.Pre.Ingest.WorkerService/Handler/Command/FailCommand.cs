using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class FailCommand : AbstractPreingestCommand
    {
        public FailCommand(ValidationActionType currentAction, ILogger<PreingestEventHubHandler> logger, Uri webApiUrl) : base(logger, webApiUrl)
        {
            ActionTypeName = currentAction;
        }

        public override ValidationActionType ActionTypeName { get; }

        public override void Execute(HttpClient client, Guid currentFolderSessionId)
        {
            TryExecuteOrCatch(client, currentFolderSessionId, (id) =>
            {
                throw new ApplicationException(String.Format ("Fail command is executed! Action for {0} will go to failed status.", ActionTypeName));
            });
        }

        public override void Execute(HttpClient client, Guid currentFolderSessionId, Settings settings)
        {
            TryExecuteOrCatch(client, currentFolderSessionId, (id) =>
            {
                throw new ApplicationException(String.Format("Fail command is executed! Action for {0} will go to failed status.", ActionTypeName));
            });
        }
    }
}
