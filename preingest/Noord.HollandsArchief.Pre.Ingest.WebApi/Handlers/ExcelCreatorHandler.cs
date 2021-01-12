using Microsoft.Extensions.Logging;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class ExcelCreatorHandler : AbstractPreingestHandler
    {
        public ExcelCreatorHandler(AppSettings settings) : base(settings) { }

        private String GetProcessingUrl(string servername, string port, Guid folderSessionId)
        {          
            //#44
            return String.Format(@"http://{0}:{1}/excelreport/{2}", servername, port, folderSessionId);
        }

        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name); 
            eventModel.Summary.Processed = 1;

            OnTrigger(new PreingestEventArgs { Description = "Start generate Excel report", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            bool isSucces = false;
            var anyMessages = new List<String>();
            try
            {               
                string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, SessionGuid);

                var filePath = Path.Combine(TargetFolder, String.Format("{0}.xlsx", this.GetType().Name));
                if (File.Exists(filePath))
                    File.Delete(filePath);

                using (WebClient client = new WebClient())
                {
                    client.DownloadFileCompleted += (object sender, System.ComponentModel.AsyncCompletedEventArgs e) => OnTrigger(new PreingestEventArgs { Description = String.Format("Download complete."), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                    client.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) => OnTrigger(new PreingestEventArgs { Description = String.Format("Download progress: '{0}' ({1} / {2})% ", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });   
                    client.DownloadFile(requestUri, filePath);
                }
                
                eventModel.Summary.Accepted = 1;
                eventModel.Summary.Rejected = 0;

                eventModel.ActionData = new string[] { filePath };

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                isSucces = true;
            }
            catch (Exception e)
            {
                isSucces = false;

                Logger.LogError(e, "An exception occured in retrieving Excel file!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in retrieving Excel file!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = 1;

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description = "An exception occured in retrieving Excel file!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description = "Generate Excel file is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }
    }
}
