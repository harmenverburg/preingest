using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebTool.Entities
{
    public class AppSettings
    {
        public String DoUnpackCollection { get; set; }
        public String GetCollections { get; set; }
        public String GetResults { get; set; }
        public String GetJson { get; set; }
        public String GetSessions { get; set; }
        public String GetSidecarTree { get; set; }
        public String GetAggregationSummary { get; set; }
        public String GetDroidSummary { get; set; }
        public String GetTopxData { get; set; }
        public String GetDroidPronomInfo { get; set; }
        public String GetMetadataEncoding { get; set; }
        public String GetGreenlistStatus { get; set; }
        public String GetChecksums { get; set; }
        public String GetSchemaResult { get; set; }

        public String UpdateBinary { get; set; }
    }
}
