using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class BodySettings
    {
        public string Description { get; set; }
        public string ChecksumType { get; set; }
        public string ChecksumValue { get; set; }
        public string PreservicaTarget { get; set; }
        public string PreservicaSecurityTag { get; set; }
    }
}
