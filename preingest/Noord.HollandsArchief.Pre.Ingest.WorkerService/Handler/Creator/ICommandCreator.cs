using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public interface ICommandCreator
    {

        public IPreingestCommand FactoryMethod(Guid guid, HttpClient client);
    }
}
