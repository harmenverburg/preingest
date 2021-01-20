using System;
using Newtonsoft.Json;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Status
{
    public class BodyUpdate
    {
        [JsonProperty(Required = Required.Always)]
        public String Result { get; set; }
        [JsonProperty(Required = Required.Always)]
        public String Summary { get; set; }
    }
}
