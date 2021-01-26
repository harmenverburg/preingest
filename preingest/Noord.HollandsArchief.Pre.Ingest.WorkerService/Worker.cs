using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly PreingestEventHubHandler _eventHandler = null;

        public Worker(ILogger<Worker> logger, PreingestEventHubHandler eventHandler)
        {
            _logger = logger;
            _eventHandler = eventHandler;
        }        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            while (!stoppingToken.IsCancellationRequested)
            {
                await _eventHandler.Connect(stoppingToken);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
