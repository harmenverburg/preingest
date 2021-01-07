using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class PreingestEventArgs : EventArgs
    {
        public String Description { get; set; }
        public PreingestActionStates ActionType { get; set; }
        public DateTime Initiate { get; set; }
        public PreingestActionModel PreingestAction { get; set; }
        public PairNode<ISidecar> SidecarStructure { get; set; }
    }
}
