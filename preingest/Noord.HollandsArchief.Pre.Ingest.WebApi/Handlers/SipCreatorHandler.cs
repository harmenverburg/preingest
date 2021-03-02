using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class SipCreatorHandler : AbstractPreingestHandler, IDisposable
    {
        public SipCreatorHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection) { }

        private String GetProcessingUrl(string servername, string port, Guid folderSessionId, string folder)
        {
            return String.Format(@"http://{0}:{1}/sipcreator/{2}/{3}", servername, port, folderSessionId, Uri.EscapeDataString(folder));
        }

        public override void Execute()
        {
            var archive = new DirectoryInfo(TargetFolder).GetDirectories().FirstOrDefault();
            if (archive == null)
            {
                Logger.LogInformation("Sip Creator : In '{0}' zijn geen onderliggende mappen gevonden. Minimaal 1 verwacht.", TargetFolder);
                throw new DirectoryNotFoundException(String.Format("Sip Creator : In '{0}' zijn geen onderliggende mappen gevonden. Minimaal 1 verwacht.", TargetFolder));
            }
            Logger.LogInformation("Sip Creator : Gevonden map '{0}'.", archive.Name);

            BodySettings settings = new SettingsReader(this.ApplicationSettings.DataFolderName, SessionGuid).GetSettings();

            if (settings == null)
                throw new ApplicationException("Settings are not saved!");

            var keyValueContent = settings.ToKeyValue();
            var formUrlEncodedContent = new FormUrlEncodedContent(keyValueContent);
            var urlEncodedString = formUrlEncodedContent.ReadAsStringAsync().Result;

            if (String.IsNullOrEmpty(urlEncodedString))
                throw new ApplicationException("Post data is empty!");

            string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, SessionGuid, archive.Name);
            StringBuilder errorMessageBuilder = new StringBuilder();
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string result = wc.UploadString(requestUri, urlEncodedString);
                }
            }
            catch (WebException e)
            {
                Logger.LogError(e, "XSLWeb SIP Creator request didn't return successfully!", e.Message);                
                errorMessageBuilder.AppendLine("XSLWeb SIP Creator request didn't return successfully!");
                errorMessageBuilder.AppendLine(e.Message);

                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    Logger.LogError(String.Format("Status Code : {0}", ((HttpWebResponse)e.Response).StatusCode));
                    Logger.LogError(String.Format("Status Description : {0}", ((HttpWebResponse)e.Response).StatusDescription));
                    errorMessageBuilder.AppendLine(String.Format("Status Code : {0}", ((HttpWebResponse)e.Response).StatusCode));
                    errorMessageBuilder.AppendLine(String.Format("Status Description : {0}", ((HttpWebResponse)e.Response).StatusDescription));

                    using (StreamReader r = new StreamReader(((HttpWebResponse)e.Response).GetResponseStream()))
                    {
                        Logger.LogError(String.Format("Content: {0}", r.ReadToEnd()));
                        errorMessageBuilder.AppendLine(String.Format("Content: {0}", r.ReadToEnd()));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "XSLWeb SIP Creator request didn't return successfully!", e.Message);
                errorMessageBuilder.AppendLine("XSLWeb SIP Creator request didn't return successfully!");
                errorMessageBuilder.AppendLine(e.Message);
            }
            finally { }

            if (errorMessageBuilder.Length > 0)
                throw new ApplicationException(errorMessageBuilder.ToString());
        }

        public void Dispose() { }
    }
}
