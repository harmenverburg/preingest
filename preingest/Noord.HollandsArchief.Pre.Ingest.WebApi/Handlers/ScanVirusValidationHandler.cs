using Microsoft.Extensions.Logging;
using nClam;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 1.0
    public class ScanVirusValidationHandler : AbstractPreingestHandler
    {
        public event EventHandler<PreingestEventArgs> PreingestEvents;
        public ScanVirusValidationHandler(AppSettings settings) : base(settings) { }

        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description=String.Format("Start scanning for virus in '{0}'.", TargetFolder), Initiate = DateTime.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel }); 

            var anyMessages = new List<String>();
            var scanResults = new List<VirusScanItem>();
            bool isSucces = false;

            try
            {                           
                string[] files = Directory.GetFiles(TargetFolder, "*.*", SearchOption.AllDirectories);
                eventModel.Summary.Processed = files.Count();

                int port = 3310;
                Int32.TryParse(ApplicationSettings.ClamServerPort, out port);
                string server = ApplicationSettings.ClamServerNameOrIp;

                var clam = new ClamClient(server, port);

                foreach (var fullFilename in files)
                {
                    this.Logger.LogInformation("Scanning file '{0}'", fullFilename);

                    var scanResult = clam.ScanFileOnServerAsync(fullFilename).Result;

                    string message = string.Empty;
                    switch (scanResult.Result)
                    {
                        case ClamScanResults.Clean:
                            message = String.Format("Bestand '{0}' is schoon.", fullFilename);
                            break;
                        case ClamScanResults.VirusDetected:
                            message = String.Format("Een virus ({0}) is gevonden in bestand {1}.", scanResult.InfectedFiles.First().VirusName, fullFilename);
                            break;
                        case ClamScanResults.Error:
                            message = String.Format("Er is een fout opgetreden in '{0}'. Fout : {1}", fullFilename, scanResult.RawResult);
                            break;
                        case ClamScanResults.Unknown:
                        default:
                            message = String.Format("Er is een onbekende fout opgetreden in '{0}'. {1}", fullFilename, scanResult.RawResult);
                            break;
                    }

                    scanResults.Add(new VirusScanItem { IsClean = (scanResult.Result == ClamScanResults.Clean), Description = message, Filename = fullFilename });
                    OnTrigger(new PreingestEventArgs {Description = String.Format("Scan file '{0}'.", fullFilename), Initiate = DateTime.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                    this.Logger.LogInformation("Scan voor virus, resultaat '{0}'", message);
                }

                eventModel.Summary.Accepted = scanResults.Where(item => item.IsClean).Count();
                eventModel.Summary.Rejected = scanResults.Where(item => !item.IsClean).Count();

                eventModel.ActionData = scanResults.ToArray();

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultName = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultName = PreingestActionResults.Success;

                isSucces = true;
            }
            catch(Exception e)
            {
                isSucces = false;
                Logger.LogError(e, "An exception occured in scan virus!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in scan virus!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = -1;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.ActionResult.ResultName = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description = "An exception occured in scan virus!", Initiate = DateTime.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description = "Scanning in folder for virus is done.", Initiate = DateTime.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }

        protected void OnTrigger(PreingestEventArgs e)
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
                    if (e.PreingestAction != null)            
                        SaveJson(new DirectoryInfo(TargetFolder), this, e.PreingestAction);                    
                }
            }
        }
    }
}
