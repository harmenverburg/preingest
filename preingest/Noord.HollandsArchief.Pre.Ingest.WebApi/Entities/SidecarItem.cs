using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class SidecarItem
    {
        public String Level { get; set; }
        public bool IsCorrect { get; set; }
        public String TitlePath { get; set; }
        public String[] ErrorMessages { get; set; }
    }
}
