using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Model
{
    public class PreIngestStatusContext : DbContext
    {
        public DbSet<ActionStates> ActionStateCollection { get; set; }
        public DbSet<PreingestAction> PreingestActionCollection { get; set; }
        public DbSet<StateMessage> ActionStateMessageCollection { get; set; }

        public PreIngestStatusContext() : base() { }

        public PreIngestStatusContext(DbContextOptions<PreIngestStatusContext> options)
            : base(options) {  }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PreingestAction>().ToTable("Actions");
            modelBuilder.Entity<ActionStates>().ToTable("States");
            modelBuilder.Entity<StateMessage>().ToTable("Messages"); 
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
