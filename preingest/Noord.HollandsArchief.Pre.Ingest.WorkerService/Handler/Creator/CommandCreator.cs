using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public abstract class CommandCreator : ICommandCreator
    {    
        public CommandCreator(ILogger<PreingestEventHubHandler> logger)
        {
            Logger = logger;
        }

        protected ILogger<PreingestEventHubHandler> Logger { get; set; }
        public abstract IPreingestCommand FactoryMethod(Guid guid, dynamic data);
    }

}
