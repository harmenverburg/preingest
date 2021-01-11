using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class EncodingItem
    {
        public bool IsUtf8 { get; set; }
        public String MetadataFile { get; set; }
        public String Description { get; set; }
    }
}
