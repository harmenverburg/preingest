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
    public class SipZipHandler : AbstractPreingestHandler, IDisposable
    {
        public SipZipHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
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
            OnTrigger(new PreingestEventArgs { Description = String.Format("Start preparing sip.zip of container '{0}'.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            string output = string.Empty;
            string error = string.Empty;

            var anyMessages = new List<String>();

            bool isCompleted = false;
            try
            {
                if (!Directory.Exists(ApplicationSettings.TransferAgentTestFolder))
                    throw new DirectoryNotFoundException(String.Format("Directory not found {0}!", ApplicationSettings.TransferAgentTestFolder));
                if (!Directory.Exists(ApplicationSettings.TransferAgentProdFolder))
                    throw new DirectoryNotFoundException(String.Format("Directory not found {0}!", ApplicationSettings.TransferAgentProdFolder));

                FileInfo sipZipFile;

                sipZipFile = Directory.GetFiles(this.TargetFolder, "*.sip.zip").Select(item => new FileInfo(item)).FirstOrDefault();
                if (sipZipFile == null)
                    throw new FileNotFoundException("File with extension *.sip.zip not found!");
                
                //unzip and retrieve metadata.xml
                String[] messages = UnzipMetadataFile(eventModel, sipZipFile);
                messages.ToList().ForEach(output => this.Logger.LogDebug(output));
                anyMessages.AddRange(messages);
                eventModel.Properties.Messages = anyMessages.ToArray();

                //validate metadata.xml file against schema
                string metadataFile = Path.Combine(this.TargetFolder, "metadata.xml");
                var validationResults = ValidateMetadataFile(metadataFile);
                //save any result messages
                eventModel.ActionData = validationResults.ToArray();
                
                var any = validationResults.Any(item => item.IsConfirmSchema == false);
                if (any)
                {
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;

                    eventModel.Summary.Processed = 1;
                    eventModel.Summary.Accepted = 0;
                    eventModel.Summary.Rejected = 1;
                    anyMessages.Add("Metadata validation is not confirm schema. Cannot continue with the transfer agent.");
                }
                else
                {
                    //start copy file from x to y
                    string prod = Path.Combine(ApplicationSettings.TransferAgentProdFolder, sipZipFile.Name);
                    string test = Path.Combine(ApplicationSettings.TransferAgentTestFolder, sipZipFile.Name);

                    OnTrigger(new PreingestEventArgs { Description = String.Format("Start copy sip.zip from {0} to {1}", sipZipFile.FullName, test), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                    var taskTest = CopyFile(sipZipFile.FullName, test);
                    var taskProd = CopyFile(sipZipFile.FullName, prod);

                    OnTrigger(new PreingestEventArgs { Description = String.Format("Start copy sip.zip from x to y"), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                    var whenall = Task.WhenAll(taskTest, taskProd);

                    OnTrigger(new PreingestEventArgs { Description = String.Format("Done copy sip.zip from x to y"), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                    if (whenall.IsCompleted && !whenall.IsCompletedSuccessfully)
                    {
                        eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                        if (whenall.Exception != null)
                        {
                            var aggrExc = whenall.Exception.Flatten();
                            anyMessages.Add(aggrExc.Message);
                        }

                        eventModel.Summary.Processed = 1;
                        eventModel.Summary.Accepted = 0;
                        eventModel.Summary.Rejected = 1;
                    }
                    else
                    {
                        anyMessages.Add(String.Format("{0} copied to {1}", sipZipFile.FullName, test));
                        anyMessages.Add(String.Format("{0} copied to {1}", sipZipFile.FullName, prod));

                        eventModel.Summary.Processed = 1;
                        eventModel.Summary.Accepted = 1;
                        eventModel.Summary.Rejected = 0;
                    }
                }

                eventModel.Properties.Messages = anyMessages.ToArray();
                //succes here means i'm completed this function without exception, finally may commit to db
                isCompleted = true;
            }
            catch (Exception e)
            {
                isCompleted = false;
                anyMessages.Clear();
                anyMessages.Add(String.Format("Preparing sip.zip file of container: '{0}' failed!", TargetCollection));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                Logger.LogError(e, "Preparing sip.zip file of container: '{0}' failed!", TargetCollection);

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                
                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = 1;

                OnTrigger(new PreingestEventArgs { Description = "An exception occured while preparing sip.zip for a container!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isCompleted)
                {                    
                    eventModel.Summary.Processed = 1;
                    eventModel.Summary.Accepted = 1;
                    eventModel.Summary.Rejected = 0;
                    OnTrigger(new PreingestEventArgs { Description = "Preparing sip.zip is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
                }
            }
        }
        private String GetProcessingUrl(string servername, string port, string pad)
        {
            string reluri = pad.Remove(0, "/data/".Length);
            return String.Format(@"http://{0}:{1}/xipvalidation/{2}", servername, port, reluri);
        }
        private String[] UnzipMetadataFile(PreingestActionModel eventModel, FileInfo sipZipFile)
        {
            string output = string.Empty;
            string error = string.Empty;

            using (var tarProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "unzip",
                    Arguments = String.Format("-jo {0}/{1} *.xml -d {0}", this.TargetFolder, sipZipFile.Name),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            })
            {
                tarProcess.Start();
                OnTrigger(new PreingestEventArgs { Description = "Retrieving metadata.xml from sip.zip.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                this.Logger.LogDebug("Retrieving metadata.xml from sip.zip '{0}'", sipZipFile.FullName);

                output = tarProcess.StandardOutput.ReadToEnd();
                error = tarProcess.StandardError.ReadToEnd();

                tarProcess.Exited += (object sender, EventArgs e) =>
                {
                    OnTrigger(new PreingestEventArgs { Description = "Done retrieving metadata.xml from sip.zip.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                };
                tarProcess.WaitForExit();
            }

            var result = new List<String>();
            var splitOutput =  output.Split(Environment.NewLine);
            var splitError = error.Split(Environment.NewLine);
            result.AddRange(splitOutput);
            result.AddRange(splitError);

            return result.Where(item => item.Trim().Length > 0).ToArray();
        }
        private List<MetadataValidationItem> ValidateMetadataFile(string metadataFile)
        {            
            if (!File.Exists(metadataFile))
                throw new FileNotFoundException(String.Format("File metadata.xml not found in folder {0}! Cannot validate without the file.", SessionGuid));

            string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, metadataFile);
            var errorMessages = new List<String>();
            var validation = new List<MetadataValidationItem>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var httpResponse = client.GetAsync(requestUri).Result;

                    if (!httpResponse.IsSuccessStatusCode)
                        throw new Exception("Failed to request data!");

                    var rootError = JsonConvert.DeserializeObject<Root>(httpResponse.Content.ReadAsStringAsync().Result);
                    if (rootError == null)
                        throw new ApplicationException("Metadata validation request failed!");

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
                            MetadataFilename = metadataFile,
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
                            MetadataFilename = metadataFile,
                            RequestUri = requestUri
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, String.Format("Exception occured in metadata validation with request '{0}' for metadata file '{1}'!", requestUri, metadataFile));
                errorMessages.Clear();
                errorMessages.Add(String.Format("Exception occured in metadata validation with request '{0}' for metadata file '{1}'!", requestUri, metadataFile));
                errorMessages.Add(e.Message);
                errorMessages.Add(e.StackTrace);

                //error
                validation.Add(new MetadataValidationItem
                {
                    IsValidated = true,
                    IsConfirmSchema = false,
                    ErrorMessages = errorMessages.ToArray(),
                    MetadataFilename = metadataFile,
                    RequestUri = requestUri
                });
            }

            return validation;
        }
        private async Task CopyFile(string sourceFile, string destinationFile)
        {
            try
            {
                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);

                using (FileStream sourceStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (FileStream destinationStream = File.Create(destinationFile))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(String.Format("Failed to copy file from {0} to {1}.", sourceFile, destinationFile), e);
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
