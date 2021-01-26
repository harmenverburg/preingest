using System;
using Newtonsoft.Json;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Status
{
    public class BodyMessage
    {
        [JsonProperty(Required = Required.Always)]
        public String Message { get; set; }
    }
}
