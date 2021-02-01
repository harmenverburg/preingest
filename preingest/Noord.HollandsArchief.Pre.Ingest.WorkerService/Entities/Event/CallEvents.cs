using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.Event
{
    public class CallEvents : EventArgs
    {
        public String ResponseMessage { get; set; }
    }
}
