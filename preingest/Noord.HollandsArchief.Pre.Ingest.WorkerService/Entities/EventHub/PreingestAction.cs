using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub
{
    public class PreingestAction
    {
        public String ActionStatus { get; set; }
        public DateTimeOffset Creation { get; set; }
        public String Description { get; set; }
        public Guid FolderSessionId { get; set; }
        public String Name { get; set; }
        public Guid ProcessId { get; set; }
        public String[] ResultFiles { get; set; }
        public SummaryItem Summary { get; set; }
        public ActionStatusItem[] States { get; set; }
    }
}
