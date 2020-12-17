using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Model
{
    public class PreIngestStatusContext : DbContext
    {
        public DbSet<ProcessStatusItem> Statuses { get; set; }
        public DbSet<PreIngestSession> Sessions { get; set; }
        public DbSet<StatusMessageItem> Messages { get; set; }

        public PreIngestStatusContext() : base() { }

        public PreIngestStatusContext(DbContextOptions<PreIngestStatusContext> options)
            : base(options) {  }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PreIngestSession>().ToTable("Sessions");
            modelBuilder.Entity<ProcessStatusItem>().ToTable("Status");
            modelBuilder.Entity<StatusMessageItem>().ToTable("Messages"); 
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json")
                   .Build();
                var connectionString = configuration.GetConnectionString("Sqlite");
                optionsBuilder.UseSqlite(connectionString);
            }
        }
    }
}
