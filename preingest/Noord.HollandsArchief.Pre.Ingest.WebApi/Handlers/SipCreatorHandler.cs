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
    public class SipCreatorHandler : AbstractPreingestHandler
    {
        AppSettings _appSettings = null;

        public SipCreatorHandler(AppSettings settings) : base(settings)
        {
            _appSettings = settings;
        }

        private String GetProcessingUrl(string servername, string port, string folder)
        {  
            return String.Format(@"http://{0}:{1}/sipcreator?reluri={2}", servername, port,  folder);
        }

        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        public override void Execute()
        {
            string targetFolder = TargetFolder;
            var archive = new DirectoryInfo(targetFolder).GetDirectories().FirstOrDefault();

            if(archive == null)
            {
                Logger.LogInformation("Sip Creator : In '{0}' zijn geen onderliggende mappen gevonden. Minimaal 1 verwacht.", targetFolder);
                return;
            }

            var failedResult = new List<ProcessResult>();            
            Logger.LogInformation("Sip Creator : Gevonden map '{0}'.", archive.Name);
            string requestUri = GetProcessingUrl(_appSettings.XslWebServerName, _appSettings.XslWebServerPort, archive.Name);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var httpResponse = client.GetAsync(requestUri).Result;

                    if (!httpResponse.IsSuccessStatusCode)
                        throw new Exception("Failed to request data!");

                    var rootError = JsonConvert.DeserializeObject<Root>(httpResponse.Content.ReadAsStringAsync().Result);

                    if (rootError == null)
                        throw new ApplicationException("Sip Creator request failed!");

                    if (rootError.SchematronValidationReport != null && rootError.SchematronValidationReport.errors != null
                        && rootError.SchematronValidationReport.errors.Count > 0)
                    {
                        var result = rootError.SchematronValidationReport.errors.Select(item => new ProcessResult(SessionGuid)
                        {
                            CollectionItem = archive.Name,
                            Code = "Not;OK;SipCreator",
                            CreationTimestamp = DateTime.Now,
                            ActionName = this.GetType().Name,
                            Messages = new string[] { item.message, item.FailedAssertLocation, item.FiredRuleContext, item.FailedAssertTest }
                        });
                        failedResult.AddRange(result);
                    }
                }
            }
            catch (Exception e)
            {
                var process = new ProcessResult(SessionGuid)
                {
                    CollectionItem = archive.Name,
                    Code = "Not;OK;SipCreator",
                    CreationTimestamp = DateTime.Now,
                    ActionName = this.GetType().Name,
                    Message = String.Format("SipCreator uitvoeren is niet gelukt!{0}{1}{0}{2}", Environment.NewLine, e.Message, e.StackTrace)
                };
                failedResult.Add(process);
            }
            
            SaveJson(new DirectoryInfo(targetFolder), this, failedResult.ToArray());
        }
    }
}
