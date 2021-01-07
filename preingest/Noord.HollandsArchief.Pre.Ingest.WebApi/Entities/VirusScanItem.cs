using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class VirusScanItem
    {
        public  bool IsClean { get; set; }
        public String Filename { get; set; }
        public String Description { get; set; }
    }
}
