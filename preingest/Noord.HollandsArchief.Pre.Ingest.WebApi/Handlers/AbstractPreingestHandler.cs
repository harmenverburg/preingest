using System;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json; 
using Newtonsoft.Json.Serialization;

using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public abstract class AbstractPreingestHandler : IPreingest
    {
        private AppSettings _settings = null;
        protected Guid _guidSessionFolder = Guid.Empty;
        private ILogger _logger = null;

        public event EventHandler<PreingestEventArgs> PreingestEvents;
        public AbstractPreingestHandler(AppSettings settings)
        {
            _settings = settings;
        }

        public AppSettings ApplicationSettings
        {
            get { return this._settings; }
        }
        public virtual void Execute()
        {
            
        }
        public Guid SessionGuid
        {
            get
            {
                return this._guidSessionFolder;
            }
        }
        public Guid ActionProcessId { get; set; }
        
        public ILogger Logger { get => _logger; set => _logger = value; }
        public void SetSessionGuid(Guid guid)
        {
            this._guidSessionFolder = guid;
            ValidateAction();
        }
        public String TarFilename { get; set; }
        protected virtual void OnTrigger(PreingestEventArgs e)
        {
            EventHandler<PreingestEventArgs> handler = PreingestEvents;
            if (handler != null)
            {
                if (e.ActionType == PreingestActionStates.Started)
                    e.PreingestAction.Summary.Start = e.Initiate;

                if (e.ActionType == PreingestActionStates.Completed || e.ActionType == PreingestActionStates.Failed)
                    e.PreingestAction.Summary.End = e.Initiate;

                handler(this, e);

                if (e.ActionType == PreingestActionStates.Completed || e.ActionType == PreingestActionStates.Failed)
                {
                    SaveJson(new DirectoryInfo(TargetFolder), e.PreingestAction.Properties.ActionName, e.PreingestAction);

                    /**
                    if(e.PreingestAction.Properties.ActionName == typeof(SidecarValidationHandler).Name && e.SidecarStructure != null)
                    {
                        SaveBinary(new DirectoryInfo(TargetFolder), e.PreingestAction.Properties.ActionName, e.SidecarStructure);
                    }
                    **/
                }
                
            }
        }
        public String TargetCollection { get => Path.Combine(ApplicationSettings.DataFolderName, TarFilename); }
        public String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }
        protected PreingestActionModel CurrentActionProperties(String collectionName, String actionName, PreingestActionResults actionResult = PreingestActionResults.None)
        {
            var eventModel = new PreingestActionModel();
            eventModel.Properties = new PreingestProperties
            {
                SessionId = SessionGuid,
                CollectionItem = collectionName,
                ActionName = actionName,
                CreationTimestamp = DateTimeOffset.Now
            };
            eventModel.ActionResult = new PreingestResult() { ResultValue = actionResult };
            eventModel.Summary = new PreingestStatisticsSummary();

            return eventModel;
        }
        protected String SaveJson(DirectoryInfo outputFolder, String typeName, object data, bool useTimestamp = false)
        {
            string fileName = new FileInfo(Path.GetTempFileName()).Name;
            if (!String.IsNullOrEmpty(typeName))
                fileName = typeName.Trim();

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
                    Creation = DateTimeOffset.Now,
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
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTimeOffset.Now, Name = "Started", ProcessId = currentSession.ProcessId, Session = currentSession };
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
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTimeOffset.Now, Name = "Completed", ProcessId = currentSession.ProcessId, Session = currentSession };
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
                    var item = new ActionStates { StatusId = Guid.NewGuid(), Creation = DateTimeOffset.Now, Name = "Failed", ProcessId = currentSession.ProcessId, Session = currentSession };
                    context.Add<ActionStates>(item);

                    try
                    {
                        context.SaveChanges();

                        if (!String.IsNullOrEmpty(message))
                        {
                            var stateMessage = new StateMessage
                            {
                                Creation = DateTimeOffset.Now,
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
        public virtual void ValidateAction()
        {
            if (SessionGuid == Guid.Empty)
                throw new ApplicationException("SessionId is empty!");

            var directory = new DirectoryInfo(ApplicationSettings.DataFolderName);
            if (!directory.Exists)
                throw new DirectoryNotFoundException(String.Format("Data folder '{0}' not found!", ApplicationSettings.DataFolderName));

            var tarArchives = directory.GetFiles("*.*").Where(s => s.Extension.EndsWith(".tar") || s.Extension.EndsWith(".gz")).Select(item
                        => new { Tar = item.Name, SessionId = ChecksumHelper.GeneratePreingestGuid(item.Name) }).ToList();

            TarFilename = tarArchives.First(item => item.SessionId == SessionGuid).Tar;

            if (String.IsNullOrEmpty(TarFilename))
                throw new ApplicationException(String.Format("Tar file not found for GUID '{0}'!", SessionGuid));

            bool exists = System.IO.Directory.Exists(Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()));
            if (!exists)
                throw new DirectoryNotFoundException(String.Format("Session {0} not found.", SessionGuid));
        }
    }
}
