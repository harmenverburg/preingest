using Microsoft.Extensions.Logging;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

using System;
using System.Net.Http;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public abstract class AbstractPreingestCommand : IPreingestCommand
    {
        public AbstractPreingestCommand(ILogger<PreingestEventHubHandler> logger, Uri webApiUrl)
        {
            Logger = logger;
            WebApi = webApiUrl;
        }

        protected ILogger<PreingestEventHubHandler> Logger { get; set; }
        protected Uri WebApi { get; set; }
        protected void TryExecuteOrCatch(Action actionMethod)
        {
            if (actionMethod == null)
                return;

            var start = DateTime.Now;
            bool isExecuted = false;
            try
            {
                actionMethod();
                isExecuted = true;
            }
            catch (Exception e)
            {
                isExecuted = false;
                Logger.LogError(e, e.Message);
            }
            finally
            {
                if (isExecuted)
                {
                    var end = DateTime.Now;
                    TimeSpan processTime = (TimeSpan)(end - start);
                }
            }
        }

        public Guid CurrentSessionId { get; set ; }
        public Settings CurrentSettings { get; set; }

        public abstract void Execute(HttpClient client);
    }
}
