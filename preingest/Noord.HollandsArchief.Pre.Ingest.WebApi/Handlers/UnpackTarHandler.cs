using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Diagnostics;
using System.IO;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class UnpackTarHandler : AbstractPreingestHandler
    {
        string _tarFile = string.Empty;       
        public UnpackTarHandler(AppSettings settings) : base (settings){ }

        public String TarFilename
        {
            set { this._tarFile = value; }
            get { return this._tarFile; }
        }      
        
        public override void Execute()
        {
            string containerFile = Path.Combine(ApplicationSettings.DataFolderName, _tarFile);

            if (!File.Exists(containerFile))
                return;
            
            string sessionFolder = Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString());

            if (!Directory.Exists(sessionFolder))
                Directory.CreateDirectory(sessionFolder);

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = String.Format("-C \"{0}\" -xvf \"{1}\"", sessionFolder, containerFile),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            this.Logger.LogDebug("Unpacking container '{0}'", containerFile);

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
                       
            process.WaitForExit();

            if (!String.IsNullOrEmpty(output))
                this.Logger.LogDebug(output);

            ProcessResult item = new ProcessResult(SessionGuid)
            {
                CollectionItem = _tarFile,
                Code = "Unpack",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Message = String.IsNullOrEmpty(error) ? output : error
            };

            SaveJson(new DirectoryInfo(sessionFolder), this, item);          
        }

        public void RemoveTarFile()
        {
            string containerFile = Path.Combine(ApplicationSettings.DataFolderName, _tarFile);

            if (File.Exists(containerFile))            
                File.Delete(containerFile);            
        }
    }
}
