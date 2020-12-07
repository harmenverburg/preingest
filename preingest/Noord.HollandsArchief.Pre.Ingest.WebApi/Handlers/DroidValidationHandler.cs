using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class DroidValidationHandler : AbstractPreingestHandler
    {
        public class StatusResult
        {
            public String Message { get; set; }
            public Boolean Result { get; set; }
        }
        public enum ReportingStyle
        {            
            Pdf,
            Droid,
            Planets
        }

        AppSettings _settings = null;

        public DroidValidationHandler(AppSettings settings) : base(settings)
        {
            this._settings = settings;
        }
                
        public override void Execute() => throw new NotSupportedException("Method is not supported in this object. Use instead GetProfiles/GetReporting/GetExporting.");
        
        public async Task<StatusResult> SetSignatureUpdate()
        {
            StatusResult result = null;
            using (HttpClient client = new HttpClient())
            {
                string url = String.Format("http://{0}:{1}/{2}", _settings.DroidServerName, _settings.DroidServerPort, "api/droid/v6.5/signature/update");
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    throw new Exception("Failed to request data!");

                result = JsonConvert.DeserializeObject<StatusResult>(await response.Content.ReadAsStringAsync());

                if (result == null || !result.Result)
                    throw new ApplicationException("Droid signature update request failed!");
            }
            return result;
        }
        
        public async Task<StatusResult> GetProfiles()
        {
            StatusResult result = null;
            using (HttpClient client = new HttpClient())
            {
                string url = String.Format("http://{0}:{1}/{2}/{3}", _settings.DroidServerName, _settings.DroidServerPort, "api/droid/v6.5/profiles/", SessionGuid);
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Failed to request data!");

                result = JsonConvert.DeserializeObject<StatusResult>(await response.Content.ReadAsStringAsync());

                if (result == null || !result.Result)
                    throw new ApplicationException("Droid profiles request failed!");
            }

            return result;
        }

        public async Task<StatusResult> GetExporting()
        {
            StatusResult result = null;
            using (HttpClient client = new HttpClient())
            {
                string url = String.Format("http://{0}:{1}/{2}/{3}", _settings.DroidServerName, _settings.DroidServerPort, "api/droid/v6.5/exporting/", SessionGuid);
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Failed to request data!");

                result = JsonConvert.DeserializeObject<StatusResult>(await response.Content.ReadAsStringAsync());

                if (result == null || !result.Result)
                    throw new ApplicationException("Droid exporting request failed!");
            }

            return result;
        }

        public async Task<StatusResult> GetReporting(ReportingStyle style)
        {
            string reportType = string.Empty;
            switch (style)
            {
                case ReportingStyle.Droid:
                    reportType = "droid";
                    break;
                case ReportingStyle.Planets:
                    reportType = "planets";
                    break;
                case ReportingStyle.Pdf:
                default:
                    reportType = "pdf";
                    break;
            }

            StatusResult result = null;
            using (HttpClient client = new HttpClient())
            {
                string url = String.Format("http://{0}:{1}/{2}/{4}/{3}", _settings.DroidServerName, _settings.DroidServerPort, "api/droid/v6.5/reporting/", SessionGuid, reportType);
                var httpResponse = await client.GetAsync(url);

                if (!httpResponse.IsSuccessStatusCode)
                    throw new Exception("Failed to request data!");

                result = JsonConvert.DeserializeObject<StatusResult>(await httpResponse.Content.ReadAsStringAsync());

                if (result == null || !result.Result)
                    throw new ApplicationException("Droid reporting request failed!");
            }

            return result;
        }
    }
}
