using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();
            services.AddHealthChecks();

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var settings = appSettingsSection.Get<AppSettings>();

            services.Add(new ServiceDescriptor(typeof(HealthCheckHandler), new HealthCheckHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(ContainerChecksumHandler), new ContainerChecksumHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(UnpackTarHandler), new UnpackTarHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(ScanVirusValidationHandler), new ScanVirusValidationHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(NamingValidationHandler), new NamingValidationHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(SidecarValidationHandler), new SidecarValidationHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(DroidValidationHandler), new DroidValidationHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(EncodingHandler), new EncodingHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(GreenListHandler), new GreenListHandler(settings)));            
            services.Add(new ServiceDescriptor(typeof(MetadataValidationHandler), new MetadataValidationHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(TransformationHandler), new TransformationHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(UpdateBinaryHandler), new UpdateBinaryHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(SpreadSheetHandler), new SpreadSheetHandler(settings)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(x => x
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader());            
            app.UseStatusCodePages();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });

            
        }
    }
}
