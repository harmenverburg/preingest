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
        Success = 3,
    }
    public class PreingestResult
    {
        public PreingestActionResults ResultName { get; set; }
    }
}
