using Newtonsoft.Json;
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

        public void Save(DirectoryInfo outputFolder, IPreIngestValidation typeName)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;
            if(typeName != null)
                fileName = typeName.GetType().Name;

            string outputFile = Path.Combine(outputFolder.FullName, String.Concat(fileName, "_", DateTime.Now.ToFileTime().ToString(), ".json"));

            using (StreamWriter file = File.CreateText(outputFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }
        }


    }
}
