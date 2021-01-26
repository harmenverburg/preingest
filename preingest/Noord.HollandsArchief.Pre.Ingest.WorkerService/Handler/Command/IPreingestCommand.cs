using System;
using System.Net.Http;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public interface IPreingestCommand
    {
        public Guid CurrentSessionId { get; set; }
        public Settings CurrentSettings { get; set; }
        public void Execute(HttpClient client);
    }
}
