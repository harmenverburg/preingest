using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command
{
    public class CalculateChecksumCommand : AbstractPreingestCommand
    {
        public CalculateChecksumCommand(Uri webapi) : base(webapi) { }
        public override void Execute(HttpClient client)
        {
            OpenAPIService.Client api = new OpenAPIService.Client(WebApi.ToString(), client);
            api.ApiPreingestCalculateAsync(this.CurrentSessionId, new OpenAPIService.BodyChecksum
            {
                ChecksumType = CurrentSettings.ChecksumType,
                InputChecksumValue = CurrentSettings.ChecksumValue
            }).GetAwaiter().GetResult();
        }
    }
}
