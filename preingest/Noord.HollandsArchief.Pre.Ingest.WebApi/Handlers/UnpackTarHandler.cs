using Microsoft.Extensions.Logging;
using Mono.Unix;
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

            string output = string.Empty;
            string error = string.Empty;

            using (var tarProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = String.Format("-C \"{0}\" -oxvf \"{1}\"", sessionFolder, containerFile),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            })
            {
                tarProcess.Start();
                
                this.Logger.LogDebug("Unpacking container '{0}'", containerFile);

                output = tarProcess.StandardOutput.ReadToEnd();
                error = tarProcess.StandardError.ReadToEnd();

                if (!String.IsNullOrEmpty(output))
                this.Logger.LogDebug(output); 
            
                tarProcess.WaitForExit();
            }

            var unixDirInfo = new UnixDirectoryInfo(sessionFolder);
            ScanPath(unixDirInfo);

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

        private void ScanPath(UnixDirectoryInfo dirinfo)
        {
            dirinfo.FileAccessPermissions = FileAccessPermissions.AllPermissions;
            foreach (var fileinfo in dirinfo.GetFileSystemEntries())
            {                
                switch (fileinfo.FileType)
                {
                    case FileTypes.RegularFile:    
                        fileinfo.FileAccessPermissions = FileAccessPermissions.AllPermissions;                    
                        break;
                    case FileTypes.Directory:
                        ScanPath((UnixDirectoryInfo)fileinfo);
                        break;
                    default:
                        /* Do nothing for symlinks or other weird things. */
                        break;
                }
            }
        }
    }
}
