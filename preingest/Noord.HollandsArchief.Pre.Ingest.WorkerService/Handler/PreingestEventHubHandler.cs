using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Model;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public class PreingestEventHubHandler : IDisposable
    {
        private HubConnection Connection { get; set; }

        private Uri WebApiUrl { get; set; }

        public PreingestEventHubHandler(String eventHubUrl, String webApiUrl)
        {
            WebApiUrl = new Uri(webApiUrl);
            Init(eventHubUrl);               
        }

        private void Init(String url)
        {
            if (String.IsNullOrEmpty(url))
                return;

            Connection = new HubConnectionBuilder()
              .WithUrl(url)
              .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10) })
              .Build();

            Connection.Closed += Closed;
            Connection.Reconnected += Reconnected;
            Connection.Reconnecting += Reconnecting;
            Connection.On<string>("SendNoticeEventToClient", (message) => Next(message));
            Connection.On<string>("PushInQueue", (message) => Push(message));

            using (var dbContext = new WorkerServiceContext())
                dbContext.Database.EnsureCreated();
        }

        private Task Reconnecting(Exception arg)
        {
            if (Connection == null)            
                CurrentLogger.LogInformation("Hub connection state is empty! Not initialised. Please check if the URL is correct.");            
            else            
                CurrentLogger.LogInformation("Hub connection state - Reconnecting - {0}", Connection.State);
            
            return Task.CompletedTask;
        }

        private Task Reconnected(string arg)
        {
            if (Connection == null)            
                CurrentLogger.LogInformation("Hub connection state is empty! Not initialised. Please check if the URL is correct.");            
            else            
                CurrentLogger.LogInformation("Hub connection state - Reconnected - {0}", Connection.State);
            
            return Task.CompletedTask;
        }

        private async Task Closed(Exception arg)
        {
            if (Connection == null)            
                CurrentLogger.LogInformation("Hub connection state is empty! Not initialised. Please check if the URL is correct.");            
            else            
                CurrentLogger.LogInformation("Hub connection state - Closed - {0}", Connection.State);
            
            await Task.Delay(5000);
        }

        private void Next(string message)
        {
            CurrentLogger.LogInformation("Hub incoming message - {0}", message);

            if (String.IsNullOrEmpty(message))
                return;

            EventMessage next = JsonConvert.DeserializeObject<EventMessage>(message);
            if (next.State != ActionStates.Completed || next.State != ActionStates.Failed)
                return;



            using (HttpClient client = new HttpClient())
            {
                OpenAPIService.Client apiService = new OpenAPIService.Client(WebApiUrl.ToString(), client);
                
            }
        }

        private void Push(string message)
        {
            CurrentLogger.LogInformation("Hub incoming message - {0}", message);

            if (String.IsNullOrEmpty(message))
                return;

            //ToDo actions
            dynamic push = JsonConvert.DeserializeObject<dynamic>(message);


        }

        public ILogger<Worker> CurrentLogger { get; set; }

        public async Task<bool> Connect(CancellationToken token)
        {
            if(Connection == null)
            {
                CurrentLogger.LogInformation("Hub connection is empty! Not initialised. Please check if the URL is correct.");
                return false;
            }

            while (true)
            {
                CurrentLogger.LogInformation("Hub connection state - Method(Connect) - {0}", Connection.State);
                if (Connection.State == HubConnectionState.Connected)
                    return true;

                try
                {
                    await Connection.StartAsync(token);
                    CurrentLogger.LogInformation("Hub connection state - Method(Connect) after StartAsync - {0}", Connection.State);
                    return true;
                }
                catch when (token.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    // Failed to connect, trying again in 5000 ms.
                    await Task.Delay(5000);
                }
            }
        }        

        public void Dispose()
        {
            if(this.Connection != null)
            {
                var dispose = Connection.DisposeAsync();
                if (dispose.GetAwaiter().IsCompleted)
                    Connection = null;                
            }
        }
    }
}
