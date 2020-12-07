using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    [Serializable]
    public class GreenListItem
    {
        /*
           "type": "Audio",
            "entension": "WAV",
            "version": "",
            "description": "",
            "puid": ""
         */
        public String Type { get; set; }
        public String Extension { get; set; }
        public String Version { get; set; }
        public String Description { get; set; }
        public String Puid { get; set; }
    }
}
