using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class SidecarItem
    {
        public String Level { get; set; }
        public bool IsCorrect { get; set; }
        public String TitlePath { get; set; }
        public String[] ErrorMessages { get; set; }
    }
}
