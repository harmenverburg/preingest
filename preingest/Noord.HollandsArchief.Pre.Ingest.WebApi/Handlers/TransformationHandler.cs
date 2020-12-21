using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class TransformationHandler : AbstractPreingestHandler
    {
        AppSettings _appSettings = null;

        public TransformationHandler(AppSettings settings) : base(settings)
        {
            _appSettings = settings;
        }

        private String GetProcessingUrl(string servername, string port, string pad)
        {
            string reluri = pad.Remove(0, "/data/".Length);
            return String.Format(@"http://{0}:{1}/transform/topx2xip?reluri={2}", servername, port,  reluri);
        }

        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        public override void Execute()
        {
            string targetFolder = TargetFolder;
            string[] metadatas = Directory.GetFiles(targetFolder, "*.metadata", SearchOption.AllDirectories);

            var failedResult = new List<ProcessResult>();

            foreach (string file in metadatas)
            { 
                Logger.LogInformation("Transformatie : {0}", file);

                //sample call: http://localhost:8080/topx2xip?reluri=8401b678-e622-475e-b382-f7c8bfd10346/Provincie%20Noord%20Holland/NL-K343625354-1/539862/539862.metadata
                string requestUri = GetProcessingUrl(_appSettings.XslWebServerName, _appSettings.XslWebServerPort, file);
                try
                {
                    WebRequest request = WebRequest.Create(requestUri);
                    using (WebResponse response = request.GetResponseAsync().Result)
                    {
                        XDocument xDoc = XDocument.Load(response.GetResponseStream());

                        if (xDoc.Root.Name.Equals("message"))
                        { 
                            failedResult.Add(new ProcessResult(SessionGuid)
                            {
                                CollectionItem = requestUri,
                                Code = "TransformXIP",
                                CreationTimestamp = DateTime.Now,
                                ActionName = this.GetType().Name,
                                Message = String.Format("XIP transformatie niet gelukt voor '{0}'. Antwoord: {1}", requestUri, xDoc.ToString()),
                            });
                        }
                        else
                        {
                            if (File.Exists(String.Concat(file, ".xip")))
                                File.Delete(String.Concat(file, ".xip"));
                            xDoc.Save(String.Concat(file, ".xip"));
                        }
                    }
                }
                catch (Exception e)
                {
                    var process = new ProcessResult(SessionGuid)
                    {
                        CollectionItem = file,
                        Code = "TransformXIP",
                        CreationTimestamp = DateTime.Now,
                        ActionName = this.GetType().Name,
                        Message = String.Format("Transformatie niet gelukt!{0}{1}{0}{2}", Environment.NewLine, e.Message, e.StackTrace)
                    };
                    failedResult.Add(process);
                }
            }

            if (failedResult.Count == 0)
                failedResult.Add(new ProcessResult(SessionGuid)
                {
                    CollectionItem = TargetFolder,
                    Code = "TransformXIP",
                    CreationTimestamp = DateTime.Now,
                    ActionName = this.GetType().Name,
                    Message = "Geen resultaten."
                });
   
                SaveJson(new DirectoryInfo(targetFolder), this, failedResult.ToArray());            
        }
    }
}
