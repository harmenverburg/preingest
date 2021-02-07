using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.OpenAPIService;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

using System;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public abstract class AbstractPreingestCommand : IPreingestCommand
    {
        public AbstractPreingestCommand(ILogger<PreingestEventHubHandler> logger, Uri webApiUrl)
        {
            Logger = logger;
            WebApi = webApiUrl;
        }

        protected ILogger<PreingestEventHubHandler> Logger { get; set; }
        protected Uri WebApi { get; set; }

        public abstract Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey.ValidationActionType ActionTypeName { get; }

        protected void TryExecuteOrCatch(HttpClient client, Guid guid, Action<Guid> actionMethod)
        {
            if (actionMethod == null)
                return;

            var start = DateTime.Now;
            bool isExecuted = false;
            try
            {
                actionMethod(guid);
                isExecuted = true;
            }
            catch (Exception e)
            {
                isExecuted = false;
                Logger.LogError(e, e.Message);
                TryAndRegisterFailedState(client, guid, e);
            }
            finally
            {
                if (isExecuted)
                {
                    var end = DateTime.Now;
                    TimeSpan processTime = (TimeSpan)(end - start);
                }
            }
        }

        protected void TryExecuteOrCatch(HttpClient client, Guid guid, Settings settings, Action<Guid, Settings> actionMethod)
        {
            if (actionMethod == null)
                return;

            var start = DateTime.Now;
            bool isExecuted = false;
            try
            {
                actionMethod(guid, settings);
                isExecuted = true;
            }
            catch (Exception e)
            {
                isExecuted = false;
                Logger.LogError(e, e.Message);
                TryAndRegisterFailedState(client, guid, e);
            }
            finally
            {
                if (isExecuted)
                {
                    var end = DateTime.Now;
                    TimeSpan processTime = (TimeSpan)(end - start);
                }
            }
        }

        public abstract void Execute(HttpClient client, Guid currentFolderSessionId);
        public abstract void Execute(HttpClient client, Guid currentFolderSessionId, Settings settings);

        private void TryAndRegisterFailedState(HttpClient client, Guid id, Exception parentExc)
        {
            try
            {
                Entities.CommandKey.ValidationActionType currentActionType = this.ActionTypeName;
                OpenAPIService.OutputClient api = new OpenAPIService.OutputClient(WebApi.ToString(), client);

                api.ProcessResponse += (object sender, Entities.Event.CallEvents e) =>
                {
                    dynamic collection = JsonConvert.DeserializeObject<dynamic>(e.ResponseMessage);

                    PreingestAction[] actions = collection.preingest == null ? new PreingestAction[] { } : JsonConvert.DeserializeObject<PreingestAction[]>(collection.preingest.ToString());

                    StringBuilder textBuilder = new StringBuilder();
                    textBuilder.AppendLine(parentExc.Message);
                    textBuilder.AppendLine(parentExc.StackTrace);
                    //clean it first before adding 2 records (started/failed) for state
                    if (actions.Length == 0)
                    {
                        CompleteNewActionRegistration(client, id, textBuilder.ToString());
                        return;
                    }

                    var currentAction = actions.FirstOrDefault(action => action.Name == currentActionType.ToString());
                    if (currentAction == null)
                    {
                        CompleteNewActionRegistration(client, id, textBuilder.ToString());
                        return;
                    }

                    if (currentAction.States == null)
                    {
                        CompleteNewStatesRegistration(client, id, currentAction.ProcessId, textBuilder.ToString());
                        return;
                    }

                    if (currentAction.States != null && currentAction.States.Length > 0)
                    {
                        if (currentAction.States.Length >= 2)
                            throw new ApplicationException(String.Format("Cannot register fault state! Action ID {0} have already 2 items in states collection. Folder/Session ID {1}", currentAction.ProcessId, id));

                        FailedStateRegistration(client, id, currentAction.ProcessId, textBuilder.ToString());
                    }
                };

                var response = api.CollectionAsync(id).GetAwaiter().GetResult();
                if (response.StatusCode != 200)
                    throw new ApplicationException(String.Format("Failed to register fault state for action {0} with ID {1}! Web API status code returned not 200 code.", currentActionType, id));
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
            }
            finally { }
        }

        private void CompleteNewActionRegistration(HttpClient client, Guid folderSessionId, string errorMessage)
        {
            Entities.CommandKey.ValidationActionType currentActionType = this.ActionTypeName;

            OpenAPIService.StatusClient status = new OpenAPIService.StatusClient(WebApi.ToString(), client);
            //add new 
            Guid processId = Guid.Empty;
            status.ProcessResponse += (object sender, Entities.Event.CallEvents e) =>
            {
                dynamic actionRegistration = JsonConvert.DeserializeObject<dynamic>(e.ResponseMessage);
                object id = actionRegistration.processId;
                bool isParsed = Guid.TryParse(id == null ? "" : id.ToString(), out processId);
            };

            var addNewActionResponse = status.NewAsync(folderSessionId, new BodyNewAction
            {
                Name = currentActionType.ToString(),
                Description = "Action created by WorkerService.",
                Result = string.Empty
            }).GetAwaiter().GetResult();

            if (processId == Guid.Empty)
                throw new ApplicationException("Parsing process (action) ID failed!");

            if (addNewActionResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("New action registration returned a bad response (not 200 code)! Action ID {0}, Folder/Session ID {1}.", processId, folderSessionId));
            //add 2 records (started/failed) for state
            var startStateResponse = status.StartAsync(processId).GetAwaiter().GetResult();
            if (startStateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Start state registration returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));

            var failedStateResponse = status.FailedAsync(processId, new BodyMessage { Message = errorMessage }).GetAwaiter().GetResult();
            if (failedStateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Failed state registration returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));

            SummaryItem summaryObj = new SummaryItem { Accepted = 0, Processed = 0, Rejected = 1, Start = DateTimeOffset.Now, End = DateTimeOffset.Now };
            string summaryStr = JsonConvert.SerializeObject(summaryObj);
            //final update
            var finalUpdateResponse = status.UpdateAsync(processId, new BodyUpdate { Result = "Failed", Summary = summaryStr }).GetAwaiter().GetResult();
            if(finalUpdateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Final update status returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));
        }

        private void CompleteNewStatesRegistration(HttpClient client, Guid folderSessionId, Guid processId, string errorMessage)
        {
            Entities.CommandKey.ValidationActionType currentActionType = this.ActionTypeName;

            OpenAPIService.StatusClient status = new OpenAPIService.StatusClient(WebApi.ToString(), client);

            //add 2 records (started/failed) for state
            var startStateResponse = status.StartAsync(processId).GetAwaiter().GetResult();
            if (startStateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Start state registration returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));

            var failedStateResponse = status.FailedAsync(processId, new BodyMessage { Message = errorMessage }).GetAwaiter().GetResult();
            if (failedStateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Failed state registration returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));

            SummaryItem summaryObj = new SummaryItem { Accepted = 0, Processed = 0, Rejected = 1, Start = DateTimeOffset.Now, End = DateTimeOffset.Now };
            string summaryStr = JsonConvert.SerializeObject(summaryObj);
            //final update
            var finalUpdateResponse = status.UpdateAsync(processId, new BodyUpdate { Result = "Failed", Summary = summaryStr }).GetAwaiter().GetResult();
            if (finalUpdateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Final update status returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));
        }
    
        private void FailedStateRegistration(HttpClient client, Guid folderSessionId, Guid processId, string errorMessage)
        {
            OpenAPIService.StatusClient status = new OpenAPIService.StatusClient(WebApi.ToString(), client);

            var failedStateResponse = status.FailedAsync(processId, new BodyMessage { Message = errorMessage }).GetAwaiter().GetResult();
            if (failedStateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Failed state registration returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));

            SummaryItem summaryObj = new SummaryItem { Accepted = 0, Processed = 0, Rejected = 1, Start = DateTimeOffset.Now, End = DateTimeOffset.Now };
            string summaryStr = JsonConvert.SerializeObject(summaryObj);
            //final update
            var finalUpdateResponse = status.UpdateAsync(processId, new BodyUpdate { Result = "Failed", Summary = summaryStr }).GetAwaiter().GetResult();
            if (finalUpdateResponse.StatusCode != 200)
                throw new ApplicationException(String.Format("Final update status returned a bad response (not 200 code)! Action ID {0}, Folder / Session ID {1}.", processId, folderSessionId));
        }
    }
}
