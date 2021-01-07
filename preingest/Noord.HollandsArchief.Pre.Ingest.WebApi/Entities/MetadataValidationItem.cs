using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
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
