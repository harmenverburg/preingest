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
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class SipZipCopyHandler : AbstractPreingestHandler, IDisposable
    {
        public SipZipCopyHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
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
            OnTrigger(new PreingestEventArgs { Description = String.Format("Start copy *.sip.zip of container '{0}'.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            string output = string.Empty;
            string error = string.Empty;

            var anyMessages = new List<String>();

            bool isCompleted = false;
            FileInfo sipZipFile;

            try
            {

                if (!Directory.Exists(ApplicationSettings.TransferAgentTestFolder))
                    throw new DirectoryNotFoundException(String.Format("Directory not found {0}!", ApplicationSettings.TransferAgentTestFolder));
                if (!Directory.Exists(ApplicationSettings.TransferAgentProdFolder))
                    throw new DirectoryNotFoundException(String.Format("Directory not found {0}!", ApplicationSettings.TransferAgentProdFolder));

                sipZipFile = Directory.GetFiles(this.TargetFolder, "*.sip.zip").Select(item => new FileInfo(item)).FirstOrDefault();
                if (sipZipFile == null)
                    throw new FileNotFoundException("File with extension *.sip.zip not found!");

                BodySettings settings = new SettingsReader(ApplicationSettings.DataFolderName, SessionGuid).GetSettings();
                if (settings == null)
                    throw new NullReferenceException("Settings is null! Is settings configuration saved?");

                if (string.IsNullOrEmpty(settings.Environment))
                    throw new ApplicationException("Environment is empty!");
                               
                //start copy file from x to y
                string prodPath = Path.Combine(ApplicationSettings.TransferAgentProdFolder, sipZipFile.Name);
                string testPath = Path.Combine(ApplicationSettings.TransferAgentTestFolder, sipZipFile.Name);

                OnTrigger(new PreingestEventArgs { Description = String.Format("Start copy *.sip.zip from {0} to {1}", sipZipFile.FullName, testPath), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
 
                Task task = null;
                OnTrigger(new PreingestEventArgs { Description = String.Format("Start copy *.sip.zip from x to y"), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                                
                if (settings.Environment.Equals("test", StringComparison.InvariantCultureIgnoreCase))                
                    task = CopyFile(sipZipFile.FullName, testPath);                

                if (settings.Environment.Equals("prod", StringComparison.InvariantCultureIgnoreCase))                
                    task = CopyFile(sipZipFile.FullName, prodPath);

                Task whenall = Task.WhenAll(task);

                OnTrigger(new PreingestEventArgs { Description = String.Format("Done copy *.sip.zip from x to y"), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

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
                    if (settings.Environment.Equals("test", StringComparison.InvariantCultureIgnoreCase))
                        anyMessages.Add(String.Format("{0} copied to {1}", sipZipFile.FullName, testPath));

                    if (settings.Environment.Equals("prod", StringComparison.InvariantCultureIgnoreCase))
                        anyMessages.Add(String.Format("{0} copied to {1}", sipZipFile.FullName, prodPath));

                    eventModel.Summary.Processed = 1;
                    eventModel.Summary.Accepted = 1;
                    eventModel.Summary.Rejected = 0;
                }                

                eventModel.Properties.Messages = anyMessages.ToArray();
                //succes here means i'm completed this function without exception, finally may commit to db
                isCompleted = true;
            }
            catch (Exception e)
            {
                isCompleted = false;
                anyMessages.Clear();
                anyMessages.Add(String.Format("Moving *.sip.zip file of container: '{0}' failed!", TargetCollection));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                Logger.LogError(e, "Moving *.sip.zip file of container: '{0}' failed!", TargetCollection);

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
                    OnTrigger(new PreingestEventArgs { Description = "Copying *.sip.zip is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
                }
            }
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
