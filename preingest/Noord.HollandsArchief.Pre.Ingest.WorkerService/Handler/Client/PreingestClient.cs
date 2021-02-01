using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.OpenAPIService
{
    public partial class PreingestClient
    {
        public async Task PrepareRequestAsync(System.Net.Http.HttpClient httpClient, System.Net.Http.HttpRequestMessage request, StringBuilder urlBuilder)
        {
            await PrepareRequestAsync(httpClient, request, urlBuilder.ToString());
        }

        public async Task PrepareRequestAsync(System.Net.Http.HttpClient httpClient, System.Net.Http.HttpRequestMessage request, String urlBuilder)
        {
            await Task.Run(() =>
            {
                // do nothing 
            });
        }

        public async Task ProcessResponseAsync(System.Net.Http.HttpClient httpClient, System.Net.Http.HttpResponseMessage response)
        {
            await Task.Run(() =>
            {
                //do nothing
            });
        }
    }
}
