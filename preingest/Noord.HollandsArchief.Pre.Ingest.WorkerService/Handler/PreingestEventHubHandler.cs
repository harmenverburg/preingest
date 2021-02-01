using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Model;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public class PreingestEventHubHandler : IDisposable
    {
        private HubConnection Connection { get; set; }
        private Uri WebApiUrl { get; set; }
        protected ILogger<PreingestEventHubHandler> CurrentLogger { get; set; }

        private ICommandCreator Creator { get; set; }
        public PreingestEventHubHandler(ILogger<PreingestEventHubHandler> logger, AppSettings appSettings)
        {
            WebApiUrl = new Uri(appSettings.WebApiUrl);
            Init(appSettings.EventHubUrl);
            CurrentLogger = logger;
            Creator = new PreingestCommandCreator(logger, WebApiUrl);            
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
            Connection.On<Guid, String>("CollectionStatus", (guid, jsonData) => RunNext(guid, jsonData));

            //using (var dbContext = new WorkerServiceContext())
                //dbContext.Database.EnsureCreated();
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

        private void RunNext(Guid guid, string jsonData)
        {
            CurrentLogger.LogInformation("Hub incoming message - {0}.", guid);
            Task.Run(() =>
            {
                try
                {
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonData);
                    IPreingestCommand command = null;
                    command = Creator.FactoryMethod(guid, data);

                    if (command != null)
                    {
                        Settings settings = data.settings == null ? null : JsonConvert.DeserializeObject<Settings>(data.settings.ToString());

                        using (HttpClient client = new HttpClient())
                        {
                            if (settings == null)
                                command.Execute(client, guid);
                            else
                                command.Execute(client, guid, settings);
                        }
                    }
                }
                catch (Exception e)
                {
                    CurrentLogger.LogInformation("An exception occurred with SessionId {0}.", guid);
                    CurrentLogger.LogError(e, e.Message);
                }
                finally { }
            });
        }

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
                {
                    GC.SuppressFinalize(Connection);
                    Connection = null;
                }
            }
        }
    }
}
