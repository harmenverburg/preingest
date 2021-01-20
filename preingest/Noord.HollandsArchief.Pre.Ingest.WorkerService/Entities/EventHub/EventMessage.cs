using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActionStates
    {
        None,
        Started,
        Executing,
        Completed,
        Failed
    }

    public class EventMessage
    {
        public DateTimeOffset EventDateTime { get; set; }
        public Guid SessionId { get; set; }
        public String Name { get; set; }
        public ActionStates State { get; set; }
        public String Message { get; set; }
        public StatsSummary Summary { get; set; }
    }
}
