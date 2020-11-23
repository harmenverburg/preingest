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
    public class ScanVirusValidationHandler : AbstractPreIngestChecksHandler
    {
        public ScanVirusValidationHandler(AppSettings settings) : base(settings)
        {
        }         

        public override void Execute()
        {
            if (SessionGuid == Guid.Empty)
                return;
            
            List<String> filesList = new List<String>();
            string targetFolder = Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString());

            DirectorySearchFiles(targetFolder, filesList);
                    
            var scanResults = new List<ProcessResult>();

            filesList.ForEach(file =>
            {
                var result = ScanFile(file);
                if (!result.Code.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
                    scanResults.Add(result);
            });

            SaveJson(new DirectoryInfo(targetFolder), this, scanResults.ToArray());
        }

        private void DirectorySearchFiles(string fullFoldername, List<String> filesList)
        {
            foreach (string directory in Directory.GetDirectories(fullFoldername))
            {
                foreach (string file in Directory.GetFiles(directory))
                    filesList.Add(file);

                DirectorySearchFiles(directory, filesList);
            }
        }

        private ProcessResult ScanFile(string fullFilename)
        {
            int port = 3310;
            Int32.TryParse(ApplicationSettings.ClamServerPort, out port);
            string server = ApplicationSettings.ClamServerNameOrIp;
            
            var clam = new ClamClient(server, port);

            this.Logger.LogDebug("Scanning file '{0}'", fullFilename);

            var scanResult = clam.ScanFileOnServerAsync(fullFilename).Result;

            ProcessResult item = new ProcessResult(SessionGuid)
            {
                CollectionItem = fullFilename.Remove(0, (fullFilename.IndexOf(SessionGuid.ToString()) + SessionGuid.ToString().Length)),
                Code = "OK",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Message = string.Empty
            }; 

            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    {
                        item.Message = String.Format("Bestand '{0}' is schoon.",  item.CollectionItem);                      
                    }
                    break;
                case ClamScanResults.VirusDetected:
                    {                        
                        item.Message = String.Format("Een virus ({0}) is gevonden in bestand {1}.", scanResult.InfectedFiles.First().VirusName, item.CollectionItem);
                        item.Code = "Not;OK;VirusFound";                                             
                    }
                    break;
                case ClamScanResults.Error:
                    {                        
                        item.Message = String.Format("Er is een fout opgetreden in '{0}'. Fout : {1}", item.CollectionItem, scanResult.RawResult);
                        item.Code = "Not;OK;ErrorOccured";
                    }
                    break;
                case ClamScanResults.Unknown:
                default:
                    {                      
                        item.Message = String.Format("Er is een onbekende fout opgetreden in '{0}'. {1}", item.CollectionItem, scanResult.RawResult);
                        item.Code = "Not;OK;UnknownError";
                    }
                    break;
            }

            this.Logger.LogInformation("Scan voor virus, resultaat '{0}'", item.Message);

            return item;
        }
    }
}
