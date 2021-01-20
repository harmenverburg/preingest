using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class BodyChecksum
    {
        [JsonProperty(Required = Required.Always)]
        public String ChecksumType { get; set; }
        [JsonProperty(Required = Required.AllowNull)]
        public String InputChecksumValue { get; set; }
    }

}
