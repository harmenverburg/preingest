using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class MetadataValidationHandler : AbstractPreingestHandler
    { 
        AppSettings _appSettings = null;

        public MetadataValidationHandler(AppSettings settings) : base(settings)
        {
            _appSettings = settings;
        }

        private String GetProcessingUrl(string servername, string port, string pad)
        {
            string reluri = pad.Remove(0, "/data/".Length);
            //topxvalidatie?reluri=Provincie%20Noord%20Holland/Provincie%20Noord%20%20Holland.metadata&format=json
            return String.Format(@"http://{0}:{1}/topxvalidatie?format=json&reluri={2}", servername, port,  reluri);
        }

        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        public override void Execute()
        {
            string targetFolder = TargetFolder;
            string sessionFolder = Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString());

            string[] metadatas = Directory.GetFiles(sessionFolder, "*.metadata", SearchOption.AllDirectories);

            var failedResult = new List<ProcessResult>();

            foreach (string file in metadatas)
            { 
                Logger.LogInformation("Transformatie : {0}", file);

                //sample call: http://localhost:8080/topx2xip?reluri=8401b678-e622-475e-b382-f7c8bfd10346/Provincie%20Noord%20Holland/NL-K343625354-1/539862/539862.metadata
                string requestUri = GetProcessingUrl(_appSettings.XslWebServerName, _appSettings.XslWebServerPort, file);
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

                        if(rootError.SchematronValidationReport != null && rootError.SchematronValidationReport.errors != null
                            && rootError.SchematronValidationReport.errors.Count > 0)
                        {
                            var result = rootError.SchematronValidationReport.errors.Select(item => new ProcessResult(SessionGuid)
                            {                               
                                CollectionItem = file,
                                Code = "Not;OK;MetadataValidation",
                                CreationTimestamp = DateTime.Now,
                                ActionName = this.GetType().Name,
                                Messages = new string[] { item.message, item.FailedAssertLocation, item.FiredRuleContext, item.FailedAssertTest }
                            });
                            failedResult.AddRange(result);
                        }
                        if (rootError.SchemaValidationReport != null && rootError.SchemaValidationReport.errors != null
                            && rootError.SchemaValidationReport.errors.Count > 0)
                        {
                            var result = rootError.SchemaValidationReport.errors.Select(item => new ProcessResult(SessionGuid)
                            {
                                CollectionItem = file,
                                Code = "Not;OK;MetadataValidation",
                                CreationTimestamp = DateTime.Now,
                                ActionName = this.GetType().Name,
                                Messages = new string[] { item.message, String.Format("Line: {0}, col: {1}", item.line, item.col) }
                            });
                            failedResult.AddRange(result);
                        }
                    }
                }
                catch (Exception e)
                {
                    var process = new ProcessResult(SessionGuid)
                    {
                        CollectionItem = file,
                        Code = "Not;OK;MetadataValidation",
                        CreationTimestamp = DateTime.Now,
                        ActionName = this.GetType().Name,
                        Message = String.Format("Validatie is niet gelukt!{0}{1}{0}{2}", Environment.NewLine, e.Message, e.StackTrace)
                    };
                    failedResult.Add(process);
                }
            }

            SaveJson(new DirectoryInfo(targetFolder), this, failedResult.ToArray());
        }
    }
}
