using Newtonsoft.Json;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class SipZipMetadataValidationHandler : AbstractPreingestHandler, IDisposable
    {
        public SipZipMetadataValidationHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }       
        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }
        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description = String.Format("Start validate *.sip.zip of container '{0}'.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            string output = string.Empty;
            string error = string.Empty;

            var anyMessages = new List<String>();

            bool isCompleted = false;
            FileInfo sipZipFile;
            try
            {      
                sipZipFile = Directory.GetFiles(this.TargetFolder, "*.sip.zip").Select(item => new FileInfo(item)).FirstOrDefault();
                if (sipZipFile == null)
                    throw new FileNotFoundException("File with extension *.sip.zip not found!");
 
                var validationResults = ValidateMetadataFile(SessionGuid);
                
                //save any result messages
                eventModel.ActionData = validationResults.ToArray();

                int success = validationResults.Count(item => item.IsConfirmSchema && item.IsValidated);
                if (success == 1)
                {
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;
                    eventModel.Summary.Processed = 1;
                    eventModel.Summary.Accepted = 1;
                    eventModel.Summary.Rejected = 0;
                }
                else
                {
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                    eventModel.Summary.Processed = 1;
                    eventModel.Summary.Accepted = 0;
                    eventModel.Summary.Rejected = 1;
                }

                isCompleted = true;
            }
            catch (Exception e)
            {
                isCompleted = false;
                anyMessages.Clear();
                anyMessages.Add(String.Format("Validating metadata.xml in *.sip.zip file of container: '{0}' failed!", TargetCollection));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                Logger.LogError(e, "Validating metadata.xml in *.sip.zip file of container: '{0}' failed!", TargetCollection);

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                
                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = 1;
                eventModel.Properties.Messages = anyMessages.ToArray();
                OnTrigger(new PreingestEventArgs { Description = "An exception occured while validating metadata.xml in *.sip.zip for a container!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isCompleted)
                    OnTrigger(new PreingestEventArgs { Description = "Validate sip.zip is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });                
            }
        }
        private String GetProcessingUrl(string servername, string port, Guid guid)
        {
            return String.Format(@"http://{0}:{1}/xipvalidation/{2}", servername, port, guid);
        }

        private List<MetadataValidationItem> ValidateMetadataFile(Guid guid)
        { 
            string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, guid);
            var errorMessages = new List<String>();
            var validation = new List<MetadataValidationItem>();

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(requestUri).Result;

                if (!httpResponse.IsSuccessStatusCode)
                    throw new Exception("Failed to request data!");

                var rootError = JsonConvert.DeserializeObject<Root>(httpResponse.Content.ReadAsStringAsync().Result);
                if (rootError == null)
                    throw new ApplicationException("Metadata validation request failed!");

                try
                {
                    //schema+validation
                    if (rootError.SchematronValidationReport != null && rootError.SchematronValidationReport.errors != null
                        && rootError.SchematronValidationReport.errors.Count > 0)
                    {
                        var messages = rootError.SchematronValidationReport.errors.Select(item => String.Concat(item.message, ", ", item.FailedAssertLocation, ", ", item.FiredRuleContext, ", ", item.FailedAssertTest)).ToArray();
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
                            MetadataFilename = "metadata.xml in *.sip.zip",
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
                            ErrorMessages = new string[0] { },
                            MetadataFilename = "metadata.xml in *.sip.zip",
                            RequestUri = requestUri
                        });
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, String.Format("Exception occured in metadata validation with request '{0}' for validating metadata file in *.sip.zip!", requestUri));
                    errorMessages.Clear();
                    errorMessages.Add(String.Format("Exception occured in metadata validation with request '{0}' for validating metadata file in *.sip.zip!", requestUri));
                    errorMessages.Add(e.Message);
                    errorMessages.Add(e.StackTrace);

                    //error
                    validation.Add(new MetadataValidationItem
                    {
                        IsValidated = true,
                        IsConfirmSchema = false,
                        ErrorMessages = errorMessages.ToArray(),
                        MetadataFilename = "metadata.xml in *.sip.zip",
                        RequestUri = requestUri
                    });
                }
            }           

            return validation;
        }

    }
}
