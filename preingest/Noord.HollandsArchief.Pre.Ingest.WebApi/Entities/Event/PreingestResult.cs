using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PreingestActionResults
    {
        None = 0,
        Error = 1,
        Failed = 2,        
        Executing = 3,
        Success = 4
    }
    public class PreingestResult
    {
        public PreingestActionResults ResultValue { get; set; }
    }
}
