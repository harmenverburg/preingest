using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PreingestActionResults
    {
        None,
        Success,
        Error,
        Failed
    }
    public enum PreingestActionStates
    {
        None,        
        Started,
        Executing,
        Completed,
        Failed
    }
    public class PreingestActionModel
    {
        public PreingestStatisticsSummary Summary { get; set; }
        public PreingestResult ActionResult { get; set; }
        public PreingestProperties Properties { get; set; }
        public object ActionData { get; set; }
    }


}
