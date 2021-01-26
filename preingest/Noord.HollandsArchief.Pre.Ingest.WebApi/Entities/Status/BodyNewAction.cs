using System;
using Newtonsoft.Json;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Status
{
    public class BodyNewAction
    {
        [JsonProperty(Required = Required.Always)]
        public String Name { get; set; }
        [JsonProperty(Required = Required.Always)]
        public String Description { get; set; }
        [JsonProperty(Required = Required.Always)]
        public String Result { get; set; }
    }
}
