using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class MetadataValidationItem
    {
        public bool IsValidated { get; set; }
        public bool IsConfirmSchema { get; set; }
        public String RequestUri { get; set; }
        public String MetadataFilename { get; set; }
        public String[] ErrorMessages { get; set; }
    }
}
