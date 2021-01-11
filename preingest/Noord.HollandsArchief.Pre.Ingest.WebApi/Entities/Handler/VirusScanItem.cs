using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class VirusScanItem
    {
        public  bool IsClean { get; set; }
        public String Filename { get; set; }
        public String Description { get; set; }
    }
}
