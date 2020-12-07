using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public abstract class AbstractPreingestHandler : IPreIngestValidation
    {
        private AppSettings _settings = null;
        protected Guid _guidSessionFolder = Guid.Empty;
        private ILogger _logger = null;

        public AbstractPreingestHandler(AppSettings settings)
        {
            _settings = settings;
        }

        protected AppSettings ApplicationSettings
        {
            get { return this._settings; }
        }

        public abstract void Execute();

        public Guid SessionGuid
        {
            get
            {
                return this._guidSessionFolder;
            }
        }
        public ILogger Logger { get => _logger; set => _logger = value; }

        public void SetSessionGuid(Guid guid)
        {
            this._guidSessionFolder = guid;
        }

        protected String SaveJson(DirectoryInfo outputFolder, IPreIngestValidation typeName, object data, bool useTimestamp = false)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;
            if (typeName != null)
                fileName = typeName.GetType().Name;

            string outputFile = useTimestamp ? Path.Combine(outputFolder.FullName, String.Concat(fileName, "_", DateTime.Now.ToFileTime().ToString(), ".json")) : Path.Combine(outputFolder.FullName, String.Concat(fileName, ".json"));

            using (StreamWriter file = File.CreateText(outputFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, data);
            }

            return outputFile;
        }

        protected String SaveJson(DirectoryInfo outputFolder, IPreIngestValidation typeName, String prefix, object data, bool useTimestamp = false)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;
            if (typeName != null)
                fileName = String.Format ("{0}_{1}", typeName.GetType().Name, prefix.Trim());

            string outputFile = useTimestamp ? Path.Combine(outputFolder.FullName, String.Concat(fileName, "_", DateTime.Now.ToFileTime().ToString(), ".json")) : Path.Combine(outputFolder.FullName, String.Concat(fileName, ".json"));

            using (StreamWriter file = File.CreateText(outputFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializer.Serialize(file, data);
            }

            return outputFile;
        }

        protected String SaveJson(String outputFile, IPreIngestValidation typeName, object data)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;
            if (typeName != null)
                fileName = typeName.GetType().Name;

            if (File.Exists(outputFile))
                File.Delete(outputFile);            

            using (StreamWriter file = File.CreateText(outputFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializer.Serialize(file, data);
            }

            return outputFile;
        }

        protected String SaveBinary(DirectoryInfo outputFolder, IPreIngestValidation typeName, PairNode<ISidecar> data, bool useTimestamp = false)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;
            if (typeName != null)
                fileName = typeName.GetType().Name;

            string outputFile = useTimestamp ? Path.Combine(outputFolder.FullName, String.Concat(fileName, "_", DateTime.Now.ToFileTime().ToString(), ".bin")) : Path.Combine(outputFolder.FullName, String.Concat(fileName, ".bin"));

            Utilities.SerializerHelper.SerializeObjectToBinaryFile<PairNode<ISidecar>>(outputFile, data, false);

            return outputFile;
        }

        protected String SaveBinary(DirectoryInfo outputFolder, String typeName, PairNode<ISidecar> data, bool useTimestamp = false)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;

            if (!String.IsNullOrEmpty(typeName))
                fileName = typeName;

            string outputFile = useTimestamp ? Path.Combine(outputFolder.FullName, String.Concat(fileName, "_", DateTime.Now.ToFileTime().ToString(), ".bin")) : Path.Combine(outputFolder.FullName, String.Concat(fileName, ".bin"));

            Utilities.SerializerHelper.SerializeObjectToBinaryFile<PairNode<ISidecar>>(outputFile, data, false);

            return outputFile;
        }
    }
}
