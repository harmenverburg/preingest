using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Newtonsoft.Json;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator;


namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler
{
    public class PreingestEventHubHandler : IDisposable
    {
        internal class BlockItem
        {
            public Guid SessionId { get; set; }
            public String Data { get; set; }
        }
        private readonly object consumeLock = new object();

        private BlockingCollection<BlockItem> _internalCollection = null;
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

            _internalCollection = new BlockingCollection<BlockItem>(10);
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
            Connection.On<Guid, String>("SendNoticeToWorkerService", (guid, jsonData) => RunNext(guid, jsonData));

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
            _internalCollection.Add(new BlockItem { SessionId = guid, Data = jsonData });
            CurrentLogger.LogInformation("Hub incoming message - {0}.", guid);
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
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                DateTime buildDate = LinkerHelper.GetLinkerTimestampUtc(assembly);

                CurrentLogger.LogInformation(String.Format("{0} version {1}. Build date and time {2}.", fvi.ProductName, fvi.ProductVersion, DateTimeOffset.FromFileTime(buildDate.ToFileTime())));
                CurrentLogger.LogInformation("Hub connection state - Method(Connect) - {0}", Connection.State);
                if (Connection.State == HubConnectionState.Connected)
                {
                    lock (consumeLock)
                    {
                        while (!_internalCollection.IsCompleted)
                        {
                            BlockItem item = _internalCollection.Take();

                            Task.Run(() =>
                            {
                                try
                                {
                                    dynamic data = JsonConvert.DeserializeObject<dynamic>(item.Data);
                                    IPreingestCommand command = Creator.FactoryMethod(item.SessionId, data);

                                    if (command != null)
                                    {
                                        Settings settings = data.settings == null ? null : JsonConvert.DeserializeObject<Settings>(data.settings.ToString());

                                        using (HttpClient client = new HttpClient())
                                        {
                                            if (settings == null)
                                                command.Execute(client, item.SessionId);
                                            else
                                                command.Execute(client, item.SessionId, settings);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    CurrentLogger.LogInformation("An exception occurred with SessionId {0}.", item.SessionId);
                                    CurrentLogger.LogError(e, e.Message);
                                }
                                finally { }
                            });
                        }
                    }
                    return true;
                }

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

            if (this._internalCollection != null)
                this._internalCollection.Dispose();
        }
    }
}
