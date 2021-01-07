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
        public String TarFilename { get; set; }

        protected String TargetCollection { get => Path.Combine(ApplicationSettings.DataFolderName, TarFilename); }

        protected String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        protected PreingestActionModel CurrentActionProperties(String collectionName, String actionName, PreingestActionResults actionResult = PreingestActionResults.None)
        {
            var eventModel = new PreingestActionModel();
            eventModel.Properties = new PreingestProperties
            {
                SessionId = SessionGuid,
                CollectionItem = collectionName,
                ActionName = actionName,
                CreationTimestamp = DateTime.Now
            };
            eventModel.ActionResult = new PreingestResult() { ResultName = actionResult };
            eventModel.Summary = new PreingestStatisticsSummary();

            return eventModel;
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
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializer.NullValueHandling = NullValueHandling.Ignore;
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
                var session = new PreingestAction
                {
                    ProcessId = processId,
                    FolderSessionId = SessionGuid,
                    Description = description,
                    Name = name,
                    Creation = DateTime.Now,
                    ResultFiles = result
                };

                context.Add<PreingestAction>(session);
                try
                {
                    context.SaveChanges();
                }
                catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                finally { }
            }
            return processId;
        }

        public void UpdateProcessAction(Guid actionId, String result, String summary)
        {
            using (var context = new PreIngestStatusContext())
            {
                var currentAction = context.Find<PreingestAction>(actionId);
                if (currentAction != null)
                {
                    if (!String.IsNullOrEmpty(result))
                        currentAction.ActionStatus = result;

                    if (!String.IsNullOrEmpty(summary))
                        currentAction.StatisticsSummary = summary;

                    try
                    {
                        context.SaveChanges();
                    }
                    catch (Exception e) { _logger.LogError(e, "An exception is throwned in {0}: '{1}'.", e.Message, e.StackTrace); }
                    finally { }
                }
            }
        }

        public void AddStartState(Guid processId)
        {
            using (var context = new PreIngestStatusContext())
            {
                var currentSession = context.Find<PreingestAction>(processId);
                if (currentSession != null)
                {
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = "Started", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ActionStates>(item);
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
                var currentSession = context.Find<PreingestAction>(processId);
                if (currentSession != null)
                {
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = "Completed", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ActionStates>(item);

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
                var currentSession = context.Find<PreingestAction>(processId);
                if (currentSession != null)
                {
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTime.Now, Name = "Failed", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ActionStates>(item);

                    try
                    {
                        context.SaveChanges();

                        if (!String.IsNullOrEmpty(message))
                        {
                            var stateMessage = new StateMessage
                            {
                                Creation = DateTime.Now,
                                Description = message,
                                MessageId = Guid.NewGuid(),
                                Status = item,
                                StatusId = item.StatusId

                            };
                            context.Add<StateMessage>(stateMessage);
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
