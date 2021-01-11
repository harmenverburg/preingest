using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class HealthCheckHandler : AbstractPreingestHandler
    {
        AppSettings _settings = null;
        public HealthCheckHandler(AppSettings settings) : base(settings)
        {
            _settings = settings;
        }

        public override void Execute()
        {        
            try
            {
                using (TcpClient clamav = new TcpClient(_settings.ClamServerNameOrIp, Int32.Parse(_settings.ClamServerPort)))
                    IsAliveClamAv = clamav.Connected;
            }
            catch
            {
                IsAliveClamAv = false;
            }

            try
            {

                using (TcpClient xslweb = new TcpClient(_settings.XslWebServerName, Int32.Parse(_settings.XslWebServerPort)))
                    IsAliveXslWeb = xslweb.Connected;
            }
            catch
            {
                IsAliveXslWeb = false;
            }

            try
            {
                using (TcpClient droid = new TcpClient(_settings.DroidServerName, Int32.Parse(_settings.DroidServerPort)))
                    IsAliveDroid = droid.Connected;
            }
            catch
            {
                IsAliveDroid = false;
            }
            
            try
            {
                using (var context = new PreIngestStatusContext())
                {
                    IsAliveDatabase = context.Database.CanConnect();   
                }
            }
            catch
            {
                IsAliveDatabase = false;
            }
        }

        public bool IsAliveDroid { get; set; }
        public bool IsAliveXslWeb { get; set; }
        public bool IsAliveClamAv { get; set; }
        public bool IsAliveDatabase { get; set; }
    }
}
