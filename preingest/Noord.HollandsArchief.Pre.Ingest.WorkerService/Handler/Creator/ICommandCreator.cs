using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public interface ICommandCreator
    {
        public IPreingestCommand FactoryMethod(EventMessage eve);
    }
}
