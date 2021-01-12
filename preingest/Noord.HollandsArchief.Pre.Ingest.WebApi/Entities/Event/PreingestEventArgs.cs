using System;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event
{
    public class PreingestEventArgs : EventArgs
    {
        public String Description { get; set; }
        public PreingestActionStates ActionType { get; set; }
        public DateTimeOffset Initiate { get; set; }
        public PreingestActionModel PreingestAction { get; set; }
        public PairNode<ISidecar> SidecarStructure { get; set; }
    }
}
