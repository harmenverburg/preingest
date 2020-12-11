
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Hosting;
using Noord.HollandsArchief.Pre.Ingest.WebTool.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebTool.Models;

namespace Noord.HollandsArchief.Pre.Ingest.WebTool
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
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var settings = appSettingsSection.Get<AppSettings>();

            services.AddSingleton<PreingestModel>(new PreingestModel(settings));

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Sidecar}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                  name: "planetsummary",
                  pattern: "/{controller=Sidecar}/{action=PlanetSummary}/{id}");
                endpoints.MapControllerRoute(
                   name: "droidsummary",
                   pattern: "/{controller=Sidecar}/{action=DroidSummary}/{id}");
                endpoints.MapControllerRoute(
                   name: "sidecarsummary",
                   pattern: "/{controller=Sidecar}/{action=AggregationSummary}/{id}");
                endpoints.MapControllerRoute(
                   name: "virusscansummary",
                   pattern: "/{controller=Sidecar}/{action=VirusscanSummary}/{id}");
                endpoints.MapControllerRoute(
                  name: "namingsummary",
                  pattern: "/{controller=Sidecar}/{action=NamingSummary}/{id}");
                endpoints.MapControllerRoute(
                  name: "topxcontent",
                  pattern: "/{controller=Sidecar}/{action=TopxContent}/{sessionId}/{treeId}");
                endpoints.MapControllerRoute(
                  name: "droidpronominfo",
                  pattern: "/{controller=Sidecar}/{action=DroidPronomInfo}/{sessionId}/{treeId}");
                endpoints.MapControllerRoute(
                  name: "metadataencoding",
                  pattern: "/{controller=Sidecar}/{action=MetadataEncoding}/{sessionId}/{treeId}");
                endpoints.MapControllerRoute(
                 name: "greenliststatus",
                 pattern: "/{controller=Sidecar}/{action=GreenlistStatus}/{sessionId}/{treeId}");
                endpoints.MapControllerRoute(
                 name: "checksums",
                 pattern: "/{controller=Sidecar}/{action=Checksums}/{sessionId}/{treeId}");
                endpoints.MapControllerRoute(
                name: "schemaresult",
                pattern: "/{controller=Sidecar}/{action=SchemaResult}/{sessionId}/{treeId}");
                endpoints.MapControllerRoute(
                 name: "updatebinary",
                 pattern: "/{controller=Sidecar}/{action=UpdateBinary}/{id}");
                endpoints.MapControllerRoute(
                 name: "generate",
                 pattern: "/{controller=Sidecar}/{action=GenerateExport}/{id}");
            });
        }
    }
}
