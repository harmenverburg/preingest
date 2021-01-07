using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class NamingItem
    {
        public bool ContainsInvalidCharacters { get; set; }
        public bool ContainsDosNames { get; set; }
        public String Name { get; set; }
        public String[] ErrorMessages { get; set; }
    }
}
