using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class PreingestStatisticsSummary
    {
        public Int32 Processed { get; set; }
        public Int32 Accepted { get; set; }
        public Int32 Rejected { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
