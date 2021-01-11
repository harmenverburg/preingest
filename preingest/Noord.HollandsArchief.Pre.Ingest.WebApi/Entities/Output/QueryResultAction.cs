using System;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output
{
    public class QueryResultAction
    {
        public String ActionStatus { get; set; }
        public DateTime Creation { get; set; }
        public String Description { get; set; }
        public Guid FolderSessionId { get; set; }
        public String Name { get; set; }
        public Guid ProcessId { get; set; }
        public String ResultFiles { get; set; }
        public PreingestStatisticsSummary Summary { get; set; }
        public QueryResultState[] States { get; set; }
    }
}
