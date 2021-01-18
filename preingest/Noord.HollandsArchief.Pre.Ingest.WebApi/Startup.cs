using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;

using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
           
            services.AddSignalR();
            services.AddHealthChecks();            

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var settings = appSettingsSection.Get<AppSettings>();
            services.AddDbContext<Model.PreIngestStatusContext>(options => options.UseSqlite(Configuration.GetConnectionString("Sqlite")));

            services.AddSingleton<PreingestEventHub>();

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
            services.Add(new ServiceDescriptor(typeof(SipCreatorHandler), new SipCreatorHandler(settings)));
            services.Add(new ServiceDescriptor(typeof(ExcelCreatorHandler), new ExcelCreatorHandler(settings)));

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen();
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

            //app.UseHttpsRedirection();
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { "index.html" }
            });

            app.UseStaticFiles();
            app.UseRouting();

            var appSettingsSection = Configuration.GetSection("AppSettings");           
            var settings = appSettingsSection.Get<AppSettings>();
            var origins = settings.WithOrigins.Split(";");

            app.UseCors(x => x.AllowAnyOrigin().AllowCredentials().AllowAnyMethod().AllowAnyHeader().WithOrigins(origins));

            app.UseStatusCodePages();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
                endpoints.MapHub<PreingestEventHub>("/preingestEventHub");
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Preingest API V1");
            });
        }
    }
}
