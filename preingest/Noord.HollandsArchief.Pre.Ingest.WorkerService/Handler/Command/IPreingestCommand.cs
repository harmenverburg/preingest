using System;
using System.Net.Http;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public interface IPreingestCommand
    {
        public ValidationActionType ActionTypeName { get; }
        public void Execute(HttpClient client, Guid currentFolderSessionId);
        public void Execute(HttpClient client, Guid currentFolderSessionId, Settings settings);
    }
}
