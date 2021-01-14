using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Status
{
    public class ActionEventMessageBody
    {
        public DateTimeOffset EventDateTime { get; set; }
        public Guid SessionId { get; set; }
        public String Name { get; set; }
        public String State { get; set; }
        public String Message { get; set; }
        public bool HasSummary { get; set; }
        public Int32 Processed { get; set; }
        public Int32 Accepted { get; set; }
        public Int32 Rejected { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
}
