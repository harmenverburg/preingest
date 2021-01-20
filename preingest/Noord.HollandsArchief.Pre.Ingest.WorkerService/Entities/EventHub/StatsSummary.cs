using System;


namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub
{
    public class StatsSummary
    {
        public Int32 Processed { get; set; }
        public Int32 Accepted { get; set; }
        public Int32 Rejected { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
