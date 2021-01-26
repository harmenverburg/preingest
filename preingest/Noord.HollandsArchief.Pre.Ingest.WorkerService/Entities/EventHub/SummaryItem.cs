using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub
{
    public class SummaryItem
    {
        public Int32 Processed { get; set; }
        public Int32 Accepted { get; set; }
        public Int32 Rejected { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
