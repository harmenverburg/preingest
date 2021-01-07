using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class EncodingItem
    {
        public bool IsUtf8 { get; set; }
        public String MetadataFile { get; set; }
        public String Description { get; set; }
    }
}
