using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;
using System;
using System.IO;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class ProcessResult
    {
        private Guid id = Guid.Empty;
        public ProcessResult(Guid guid)
        {
            id = guid;
        }
        public Guid SessionId { get => id; }
        public String Code { get; set; }
        public String ActionName { get; set; }
        public String CollectionItem { get; set; }
        public String Message { get; set; }
        public String[] Messages { get; set; }
        public DateTime CreationTimestamp { get; set; } 
    }
}
