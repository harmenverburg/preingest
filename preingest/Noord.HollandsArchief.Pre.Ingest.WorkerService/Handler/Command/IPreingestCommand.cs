using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public interface IPreingestCommand
    {
        public void Execute(HttpClient client);
    }
}
