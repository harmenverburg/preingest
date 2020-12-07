using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebTool.Entities
{
    public class IndexViewModel
{ 
        public IEnumerable<Guid> Sessions { get; set; }
        public IEnumerable<String> Collections { get; set; }

    }
}
