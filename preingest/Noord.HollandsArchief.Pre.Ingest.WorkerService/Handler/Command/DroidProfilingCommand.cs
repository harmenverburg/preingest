using System;
using System.Net.Http;


namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class DroidProfilingCommand : AbstractPreingestCommand
    {
        public DroidProfilingCommand(Uri webapi) : base(webapi) { }
        public override void Execute(HttpClient client)
        {

        }
    }
}
