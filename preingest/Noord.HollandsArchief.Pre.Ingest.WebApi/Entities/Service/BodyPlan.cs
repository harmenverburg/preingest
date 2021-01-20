using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Service
{
    public class BodyPlan
    {
        public Guid SessionId { get; set; }
        public Int32 ExecutionOrder { get; set; }
        public String ActionName { get; set; }
        public bool ContinueOnFailed { get; set; }
        public bool ContinueOnError { get; set; }
    }
}
