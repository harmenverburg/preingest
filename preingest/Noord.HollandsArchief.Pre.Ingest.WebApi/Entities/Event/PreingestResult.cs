using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PreingestActionResults
    {
        None = 0,
        Executing = 1,
        Error = 2,
        Failed = 3,
        Success = 4
    }
    public class PreingestResult
    {
        public PreingestActionResults ResultValue { get; set; }
    }
}
