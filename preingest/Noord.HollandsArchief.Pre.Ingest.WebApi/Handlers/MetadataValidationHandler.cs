using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class MetadataValidationHandler : AbstractPreingestHandler, IDisposable
    {
        public MetadataValidationHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }

        private String GetProcessingUrl(string servername, string port, string pad)
        {
            string data = this.ApplicationSettings.DataFolderName.EndsWith("/") ? this.ApplicationSettings.DataFolderName : this.ApplicationSettings.DataFolderName + "/";
            string reluri = pad.Remove(0, data.Length);
            string newUri = String.Join("/", reluri.Split("/", StringSplitOptions.None).Select(item => Uri.EscapeDataString(item)));
            return String.Format(@"http://{0}:{1}/topxvalidation/{2}", servername, port, newUri);
        }

        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description="Start validate .metadata files.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            var anyMessages = new List<String>();
            bool isSucces = false;
            var validation = new List<MetadataValidationItem>();
            try
            {
                string sessionFolder = Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString());
                string[] metadatas = Directory.GetFiles(sessionFolder, "*.metadata", SearchOption.AllDirectories);

                eventModel.Summary.Processed = metadatas.Count();

                foreach (string file in metadatas)
                {
                    Logger.LogInformation("Metadata validatie : {0}", file);

                    string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, file);
                    var errorMessages = new List<String>();

                    using (HttpClient client = new HttpClient())
                    {
                        var httpResponse = client.GetAsync(requestUri).Result;

                        if (!httpResponse.IsSuccessStatusCode)
                            throw new Exception("Failed to request data! Status code not equals 200.");

                        var rootError = JsonConvert.DeserializeObject<Root>(httpResponse.Content.ReadAsStringAsync().Result);
                        if (rootError == null)
                            throw new ApplicationException("Metadata validation request failed!");

                        try
                        {
                            //schema+ validation
                            if (rootError.SchematronValidationReport != null && rootError.SchematronValidationReport.errors != null
                                && rootError.SchematronValidationReport.errors.Count > 0)
                            {
                                var messages = rootError.SchematronValidationReport.errors.Select(item => item.message).ToArray();
                                errorMessages.AddRange(messages);
                            }
                            //default schema validation
                            if (rootError.SchemaValidationReport != null && rootError.SchemaValidationReport.errors != null
                                && rootError.SchemaValidationReport.errors.Count > 0)
                            {
                                var messages = rootError.SchemaValidationReport.errors.Select(item => String.Concat(item.message, ", ", String.Format("Line: {0}, col: {1}", item.line, item.col))).ToArray();
                                errorMessages.AddRange(messages);
                            }

                            if (errorMessages.Count > 0)
                            {
                                //error
                                validation.Add(new MetadataValidationItem
                                {
                                    IsValidated = true,
                                    IsConfirmSchema = false,
                                    ErrorMessages = errorMessages.ToArray(),
                                    MetadataFilename = file,
                                    RequestUri = requestUri
                                });
                            }
                            else
                            {
                                //no error
                                validation.Add(new MetadataValidationItem
                                {
                                    IsValidated = true,
                                    IsConfirmSchema = true,
                                    ErrorMessages = new string[0],
                                    MetadataFilename = file,
                                    RequestUri = requestUri
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, String.Format("Exception occured in metadata validation with request '{0}' for metadata file '{1}'!", requestUri, file));
                            errorMessages.Clear();
                            errorMessages.Add(String.Format("Exception occured in metadata validation with request '{0}' for metadata file '{1}'!", requestUri, file));
                            errorMessages.Add(e.Message);
                            errorMessages.Add(e.StackTrace);

                            //error
                            validation.Add(new MetadataValidationItem
                            {
                                IsValidated = true,
                                IsConfirmSchema = false,
                                ErrorMessages = errorMessages.ToArray(),
                                MetadataFilename = file,
                                RequestUri = requestUri
                            });
                        }
                    }

                    OnTrigger(new PreingestEventArgs { Description = String.Format("Processing file '{0}'", file), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                }

                eventModel.Summary.Accepted = validation.Where(item => item.IsValidated && item.IsConfirmSchema).Count();
                eventModel.Summary.Rejected = validation.Where(item => !item.IsValidated || !item.IsConfirmSchema).Count();

                eventModel.ActionData = validation.ToArray();

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                isSucces = true;
            }
            catch(Exception e)
            {
                isSucces = false;
                Logger.LogError(e, "An exception occured in metadata validation!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in metadata validation!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                //eventModel.Summary.Processed = 0;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description= "An exception occured in metadata validation!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description = "Validation is done!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }

        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }
    }
}
