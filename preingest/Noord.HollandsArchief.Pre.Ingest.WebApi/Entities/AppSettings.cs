using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class AppSettings
    {
        public String WithOrigins { get; set; }
        public String DataFolderName { get; set; }
        public String ClamServerNameOrIp { get; set; }
        public String ClamServerPort { get; set; }
        public String XslWebServerName { get; set; }
        public String XslWebServerPort { get; set; }
        public String DroidServerName { get; set; }
        public String DroidServerPort { get; set; }
        public String TransferAgentTestFolder { get; set; }
        public String TransferAgentProdFolder { get; set; }
    }
}
