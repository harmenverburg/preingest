using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public abstract class CommandCreator : ICommandCreator
    {        
        public abstract IPreingestCommand FactoryMethod(Guid guid, dynamic data);
    }

}
