using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public abstract class CommandCreator : ICommandCreator
    {
        public abstract IPreingestCommand FactoryMethod(EventMessage eve);
    }

}
