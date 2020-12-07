using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebTool.Entities
{
    public class ProcessResult
    {
        public Guid SessionId { get; set; }
        public String Code { get; set; }
        public String ActionName { get; set; }
        public String CollectionItem { get; set; }
        public String Message { get; set; }
        public String[] Messages { get; set; }
        public DateTime CreationTimestamp { get; set; }
    }
}
