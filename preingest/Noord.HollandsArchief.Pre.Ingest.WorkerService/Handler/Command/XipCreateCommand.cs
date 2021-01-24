using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class XipCreateCommand : AbstractPreingestCommand
    {
        public XipCreateCommand(Uri webapi) : base(webapi) { }
        public override void Execute(HttpClient client)
        {
            OpenAPIService.Client api = new OpenAPIService.Client(WebApi.ToString(), client);
            api.ApiPreingestTransformAsync(this.CurrentSessionId).GetAwaiter().GetResult();
        }
    }
}

