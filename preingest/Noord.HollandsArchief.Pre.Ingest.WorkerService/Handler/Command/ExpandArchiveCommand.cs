using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public class ExpandArchiveCommand : AbstractPreingestCommand
    {
        public ExpandArchiveCommand(Uri webapi) : base(webapi) { }
        public override void Execute(HttpClient client)
        {
            
        }
    }
}
