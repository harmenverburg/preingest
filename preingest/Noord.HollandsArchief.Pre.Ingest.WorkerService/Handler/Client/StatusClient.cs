using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.OpenAPIService
{
    public partial class StatusClient
    {
        public event EventHandler<CallEvents> ProcessResponse;

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
