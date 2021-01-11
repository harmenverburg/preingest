using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output
{
    public class QueryResultState
    {
        public Guid StatusId { get; set; }
        public String Name { get; set; }
        public DateTime Creation { get; set; }
    }
}
