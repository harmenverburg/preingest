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
        public string Environment { get; set; }
        public string Owner { get; set; }
        public string SecurityTag { get; set; }
        public string CollectionStatus { get; set; }
        public string CollectionCode { get; set; }
        public string CollectionTitle { get; set; }
        public string CollectionRef { get; set; }
        public string Prewash { get; set; }
    }
}
