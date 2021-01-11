using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler
{
    public class NamingItem
    {
        public bool ContainsInvalidCharacters { get; set; }
        public bool ContainsDosNames { get; set; }
        public String Name { get; set; }
        public String[] ErrorMessages { get; set; }
    }
}
