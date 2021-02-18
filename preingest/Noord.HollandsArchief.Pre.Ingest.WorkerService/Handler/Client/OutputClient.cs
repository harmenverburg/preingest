using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.OpenAPIService
{
    public partial class OutputClient
    {
        public event EventHandler<CallEvents> ProcessResponse;

        public OutputClient(string url, System.Net.Http.HttpClient httpClient)
        {
            BaseUrl = url;
            _httpClient = httpClient;
            _settings = new System.Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);
        }

        protected virtual void OnTrigger(CallEvents e)
        {
            EventHandler<CallEvents> handler = ProcessResponse;
            if (handler != null)
                handler(this, e);            
        }
                
        public async Task PrepareRequestAsync(System.Net.Http.HttpClient httpClient, System.Net.Http.HttpRequestMessage request, StringBuilder urlBuilder)
        {
            await PrepareRequestAsync(httpClient, request, urlBuilder.ToString());
        }
        public async Task PrepareRequestAsync(System.Net.Http.HttpClient httpClient, System.Net.Http.HttpRequestMessage request, String urlBuilder)
        {
            await Task.Run(() =>
            {
                //do nothing
            });
        }

        public async Task ProcessResponseAsync(System.Net.Http.HttpClient httpClient, System.Net.Http.HttpResponseMessage response)
        {
            OnTrigger(new CallEvents { ResponseMessage = await response.Content.ReadAsStringAsync() });
        }
    }
}
