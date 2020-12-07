using Newtonsoft.Json;
using Noord.HollandsArchief.Pre.Ingest.WebTool.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Noord.HollandsArchief.Pre.Ingest.WebTool.Models
{
    public class PreingestModel
    {
        public PreingestModel(AppSettings settings) 
        {
            Settings = settings;
        }

        public AppSettings Settings { get; set; }

        public List<Guid> GetSessions()
        {
            Guid[] result = null;

            if (String.IsNullOrEmpty(Settings.GetSessions))
                throw new ApplicationException("Appsettings is missing value for GetSessions.");

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(Settings.GetSessions).Result;
                result = JsonConvert.DeserializeObject<Guid[]>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return new List<Guid>(result);
        }

        public List<String> GetCollections()
        {
            String[] result = null;

            if (String.IsNullOrEmpty(Settings.GetCollections))
                throw new ApplicationException("Appsettings is missing value for GetCollections.");

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(Settings.GetCollections).Result;
                result = JsonConvert.DeserializeObject<String[]>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return new List<String>(result);

        }

        public ApiResponseMessage DoUnpackCollection(String name)
        {
            ApiResponseMessage result = null;

            if (String.IsNullOrEmpty(Settings.DoUnpackCollection))
                throw new ApplicationException("Appsettings is missing value for DoUnpackCollection.");

            string url = String.Format("{0}/{1}", Settings.DoUnpackCollection, name);
            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<ApiResponseMessage>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

        public dynamic GetSidecarTree(Guid sessionId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.GetSidecarTree))
                throw new ApplicationException("Appsettings is missing value for GetSidecarTree.");

            String url = Path.Combine(Settings.GetSidecarTree, sessionId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }
            
            return result;
        }

        public dynamic GetAggregationSummary(Guid sessionId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.GetAggregationSummary))
                throw new ApplicationException("Appsettings is missing value for GetAggregationSummary.");

            String url = Path.Combine(Settings.GetAggregationSummary, sessionId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }
        
        public dynamic GetDroidAndPlanetSummary(Guid sessionId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.GetDroidSummary))
                throw new ApplicationException("Appsettings is missing value for GetDroidSummary.");

            String url = Path.Combine(Settings.GetDroidSummary, sessionId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }
            
            return result;
        }

        public dynamic GetVirusscanResult(Guid sessionId)
        {
            List<String> result = null;

            if (String.IsNullOrEmpty(Settings.GetResults))
                throw new ApplicationException("Appsettings is missing value for GetResults.");
            if (String.IsNullOrEmpty(Settings.GetJson))
                throw new ApplicationException("Appsettings is missing value for GetJson.");

            String url = Path.Combine(Settings.GetResults, sessionId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<List<String>>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            var data = result.FirstOrDefault(item => item.StartsWith("ScanVirusValidationHandler"));
            if (data == null)
            { return new { Message = "ScanVirusValidationHandler.json not found!" }; }

            String url2 = Path.Combine(Settings.GetJson, sessionId.ToString(), data);

            List<ProcessResult> output = null;
            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url2).Result;
                output = JsonConvert.DeserializeObject<List<ProcessResult>>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return output;
        }

        public dynamic GetNamingCheckResult(Guid sessionId)
        {
            List<String> result = null;

            if (String.IsNullOrEmpty(Settings.GetResults))
                throw new ApplicationException("Appsettings is missing value for GetResults.");
            if (String.IsNullOrEmpty(Settings.GetJson))
                throw new ApplicationException("Appsettings is missing value for GetJson.");

            String url = Path.Combine(Settings.GetResults, sessionId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<List<String>>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            var data = result.FirstOrDefault(item => item.StartsWith("NamingValidationHandler"));
            if (data == null)
            { return new { Message = "NamingValidationHandler.json not found!" }; }

            String url2 = Path.Combine(Settings.GetJson, sessionId.ToString(), data);

            List<ProcessResult> output = null;
            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url2).Result;
                output = JsonConvert.DeserializeObject<List<ProcessResult>>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return output;
        }

        public dynamic GetTopxData(Guid sessionId, Guid treeId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.GetTopxData))
                throw new ApplicationException("Appsettings is missing value for GetTopxData.");

            String url = Path.Combine(Settings.GetTopxData, sessionId.ToString(), treeId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

        public List<dynamic> GetDroidPronomInfo(Guid sessionId, Guid treeId)
        {
            List<dynamic> result = new List<dynamic>();

            if (String.IsNullOrEmpty(Settings.GetDroidPronomInfo))
                throw new ApplicationException("Appsettings is missing value for GetDroidExportInfo.");

            String url = Path.Combine(Settings.GetDroidPronomInfo, sessionId.ToString(), treeId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                var output = JsonConvert.DeserializeObject<List<dynamic>>(httpResponse.Content.ReadAsStringAsync().Result);

                result.AddRange(output);
            }

            return result;
        }

        public dynamic GetMetadataEncoding(Guid sessionId, Guid treeId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.GetMetadataEncoding))
                throw new ApplicationException("Appsettings is missing value for GetMetadataEncoding.");

            String url = Path.Combine(Settings.GetMetadataEncoding, sessionId.ToString(), treeId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

        public dynamic GetGreenlistStatus(Guid sessionId, Guid treeId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.GetGreenlistStatus))
                throw new ApplicationException("Appsettings is missing value for GetGreenlistStatus.");

            String url = Path.Combine(Settings.GetGreenlistStatus, sessionId.ToString(), treeId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

        public dynamic GetChecksums(Guid sessionId, Guid treeId)
        {
            if (String.IsNullOrEmpty(Settings.GetChecksums))
                throw new ApplicationException("Appsettings is missing value for GetChecksums.");

            dynamic result = null;

            String url = Path.Combine(Settings.GetChecksums, sessionId.ToString(), treeId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

        public List<ProcessResult> GetSchemaResult(Guid sessionId, Guid treeId)
        {
            if (String.IsNullOrEmpty(Settings.GetSchemaResult))
                throw new ApplicationException("Appsettings is missing value for GetSchemaResult.");

            dynamic result = null;

            String url = Path.Combine(Settings.GetSchemaResult, sessionId.ToString(), treeId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.GetAsync(url).Result;
                result = JsonConvert.DeserializeObject<List<ProcessResult>>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

        public dynamic UpdateBinary(Guid sessionId)
        {
            dynamic result = null;

            if (String.IsNullOrEmpty(Settings.UpdateBinary))
                throw new ApplicationException("Appsettings is missing value for UpdateBinary.");

            String url = Path.Combine(Settings.UpdateBinary, sessionId.ToString());

            using (HttpClient client = new HttpClient())
            {
                var httpResponse = client.PostAsync(url, new StringContent("", Encoding.Default, "application/json")).Result;

                result = JsonConvert.DeserializeObject<dynamic>(httpResponse.Content.ReadAsStringAsync().Result);
            }

            return result;
        }

    }
}
