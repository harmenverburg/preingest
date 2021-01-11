using System;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.EventHub
{
    public class EventHubMessage
    {
        public DateTime EventDateTime { get; set; }
        public Guid SessionId { get; set; }
        public String Name { get; set; }
        public PreingestActionStates State { get; set; }
        public String Message { get; set; }
        public PreingestStatisticsSummary Summary { get; set; }
    }
}
