using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public abstract class AbstractPreingestCommand : IPreingestCommand
    {
        protected Uri WebApi { get; set; }

        public Guid CurrentSessionId { get; set ; }
        public Settings CurrentSettings { get; set; }

        public AbstractPreingestCommand(Uri webApiUrl)
        {
            WebApi = webApiUrl;
        }

        public abstract void Execute(HttpClient client);
    }
}
