using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using Mono.Unix;

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class UnpackTarHandler : AbstractPreingestHandler, IDisposable
    {
        public UnpackTarHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            PreingestEvents += Trigger;
        }
        public void Dispose()
        {
            PreingestEvents -= Trigger;
        }
        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);  
            OnTrigger(new PreingestEventArgs { Description= String.Format("Start expanding container '{0}'.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });
               
            string output = string.Empty;
            string error = string.Empty;

            var anyMessages = new List<String>();
            bool isSuccess = false;
            try
            {                
                if (!File.Exists(TargetCollection))
                    throw new FileNotFoundException ("Collection not found!", TargetCollection) ;

                string sessionFolder = Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString());

                using (var tarProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = String.Format("-C \"{0}\" -oxvf \"{1}\"", sessionFolder, TargetCollection),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                })
                {
                    tarProcess.Start();
                    OnTrigger(new PreingestEventArgs { Description="Container is expanding content.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                    this.Logger.LogDebug("Unpacking container '{0}'", TargetCollection);

                    output = tarProcess.StandardOutput.ReadToEnd();
                    error = tarProcess.StandardError.ReadToEnd();

                    tarProcess.WaitForExit();
                }

                if (!String.IsNullOrEmpty(output))
                    this.Logger.LogDebug(output);

                if (!String.IsNullOrEmpty(error))
                    this.Logger.LogDebug(error);

                var fileInformation = new FileInfo(TargetCollection);
                anyMessages.Add(String.Concat("Name : ", fileInformation.Name));
                anyMessages.Add(String.Concat("Extension : ", fileInformation.Extension));
                anyMessages.Add(String.Concat("Size : ", fileInformation.Length));
                anyMessages.Add(String.Concat("CreationTime : ", fileInformation.CreationTimeUtc));
                anyMessages.Add(String.Concat("LastAccessTime : ", fileInformation.LastAccessTimeUtc));
                anyMessages.Add(String.Concat("LastWriteTime : ", fileInformation.LastWriteTimeUtc));

                eventModel.Properties.Messages = anyMessages.ToArray();
                    
                bool isWindows = RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                if (!isWindows)
                {
                    var unixDirInfo = new UnixDirectoryInfo(sessionFolder);
                    //trigger event executing
                    var passEventArgs = new PreingestEventArgs { Description = String.Format("Execute chmod 777 for container '{0}'", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel };
                    OnTrigger(passEventArgs);
                    ScanPath(unixDirInfo, passEventArgs);
                }

                isSuccess = true;
            }
            catch(Exception e)
            {
                isSuccess = false;
                anyMessages.Clear();
                anyMessages.Add(String.Format("Unpack container file: '{0}' failed!", TargetCollection));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                Logger.LogError(e, "Unpack container file: '{0}' failed!", TargetCollection);
                
                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();
                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = 1;

                OnTrigger(new PreingestEventArgs {Description = "An exception occured while unpacking a container!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSuccess)
                {                    
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;                    
                    eventModel.Summary.Processed = 1;
                    eventModel.Summary.Accepted = 1;
                    eventModel.Summary.Rejected = 0;
                    eventModel.ActionData = output.Split(Environment.NewLine);

                    OnTrigger(new PreingestEventArgs {Description="Unpacking the container is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
                }
            }
        }
        private void ScanPath(UnixDirectoryInfo dirinfo, PreingestEventArgs passEventArgs)
        {
            passEventArgs.Description = String.Format("Processing folder '{0}'.", dirinfo.FullName);
            OnTrigger(passEventArgs);
            dirinfo.FileAccessPermissions = FileAccessPermissions.AllPermissions;
            foreach (var fileinfo in dirinfo.GetFileSystemEntries())
            {                
                switch (fileinfo.FileType)
                {
                    case FileTypes.RegularFile:    
                        fileinfo.FileAccessPermissions = FileAccessPermissions.AllPermissions;                    
                        break;
                    case FileTypes.Directory:
                        ScanPath((UnixDirectoryInfo)fileinfo, passEventArgs);
                        break;
                    default:
                        /* Do nothing for symlinks or other weird things. */
                        break;
                }
            }
        }
    }
}
