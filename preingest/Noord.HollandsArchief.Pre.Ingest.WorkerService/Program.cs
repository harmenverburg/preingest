using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Model;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    //create this service instance
                    services.AddHostedService<Worker>();

                    var appSettingsSection = hostContext.Configuration.GetSection("AppSettings");
                    services.Configure<AppSettings>(appSettingsSection);

                    var settings = appSettingsSection.Get<AppSettings>();
                    //create database instance
                    services.AddDbContext<WorkerServiceContext>(options => options.UseSqlite(hostContext.Configuration.GetConnectionString("Sqlite")));
                                        
                    //create event hub
                    services.Add(new ServiceDescriptor(typeof(PreingestEventHubHandler), new PreingestEventHubHandler(settings.EventHubUrl, settings.WebApiUrl)));                   

                }).ConfigureAppConfiguration((hostingContext, config) =>
                 {
                     var env = hostingContext.HostingEnvironment;
                     config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);//optional extra provider

                     if (env.IsDevelopment()) { }//different providers in dev                     

                     config.AddEnvironmentVariables();//overwrites previous values

                     if (args != null)
                         config.AddCommandLine(args);
                 });    
    }
}
