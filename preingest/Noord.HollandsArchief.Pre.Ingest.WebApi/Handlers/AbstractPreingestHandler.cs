using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using System;
using System.IO;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public abstract class AbstractPreingestHandler : PreIngest
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

        protected String SaveJson(DirectoryInfo outputFolder, PreIngest typeName, object data, bool useTimestamp = false)
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

        protected String SaveJson(DirectoryInfo outputFolder, PreIngest typeName, String prefix, object data, bool useTimestamp = false)
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

        protected String SaveJson(String outputFile, PreIngest typeName, object data)
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

        protected String SaveBinary(DirectoryInfo outputFolder, PreIngest typeName, PairNode<ISidecar> data, bool useTimestamp = false)
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

        public Guid AddProcessAction(String name, String description, String result)
        {
            var processId = Guid.NewGuid();

            if (String.IsNullOrEmpty(description) || String.IsNullOrEmpty(result))
                return Guid.Empty;
            
            using (var context = new PreIngestStatusContext())
            {
                var session = new PreIngestSession
                {
                    ProcessId = processId,
                    FolderSessionId = SessionGuid,
                    Description = description,
                    Name = name,
                    Creation = DateTime.Now,
                    ResultFiles = result
                };

                context.Add<PreIngestSession>(session);
                try
                {
                    context.SaveChanges();
                }
                catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                finally { }
            }

            return processId;
        }

        public void AddStartState(Guid processId)
        {
            using (var context = new PreIngestStatusContext())
            {
                var currentSession = context.Find<PreIngestSession>(processId);
                if (currentSession != null)
                {
                    var item = new ProcessStatusItem { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = "Started", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ProcessStatusItem>(item);
                    try
                    {
                        context.SaveChanges();
                    }
                    catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                    finally { }
                }
            }
        }

        public void AddCompleteState(Guid processId)
        {
            using (var context = new PreIngestStatusContext())
            {
                var currentSession = context.Find<PreIngestSession>(processId);
                if (currentSession != null)
                {
                    var item = new ProcessStatusItem { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = "Completed", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ProcessStatusItem>(item);

                    try
                    {
                        context.SaveChanges();
                    }
                    catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                    finally { }
                }
            }
        }

        public void AddFailedState(Guid processId, string message)
        {
            using (var context = new PreIngestStatusContext())
            {
                var currentSession = context.Find<PreIngestSession>(processId);
                if (currentSession != null)
                {
                    var item = new ProcessStatusItem { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = "Failed", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ProcessStatusItem>(item);

                    try
                    {
                        context.SaveChanges();

                        if (!String.IsNullOrEmpty(message))
                        {
                            var stateMessage = new StatusMessageItem
                            {
                                Creation = DateTime.Now,
                                Description = message,
                                MessageId = Guid.NewGuid(),
                                Status = item,
                                StatusId = item.StatusId

                            };
                            context.Add<StatusMessageItem>(stateMessage);
                        }

                        context.SaveChanges();
                    }
                    catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                    finally { }
                }
            }
        }
    }
}
