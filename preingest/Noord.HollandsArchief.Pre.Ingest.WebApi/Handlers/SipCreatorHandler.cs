using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using System;
using System.IO;
using System.Net;
using System.Linq;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class SipCreatorHandler : AbstractPreingestHandler, IDisposable
    {
        public SipCreatorHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection) { }

        private String GetProcessingUrl(string servername, string port, Guid folderSessionId, string folder)
        {
            return String.Format(@"http://{0}:{1}/sipcreator/{2}/{3}", servername, port, folderSessionId, folder);
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
           
            string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, SessionGuid, archive.Name);
            WebRequest request = WebRequest.Create(requestUri);

            bool statusCodeOk = false;
            using (WebResponse response = request.GetResponse())
            {
                // Success
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                statusCodeOk = (httpResponse.StatusCode == HttpStatusCode.OK);                   
            }

            if(!statusCodeOk)
                throw new ApplicationException(String.Format("XSLWeb SIP Creator request '{0}' didn't return HTTP status code OK/200!", requestUri));
        }

        public void Dispose()
        {           
        }
    }
}
