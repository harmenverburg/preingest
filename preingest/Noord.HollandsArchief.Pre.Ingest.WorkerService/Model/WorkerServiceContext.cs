using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System.IO;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Model
{
    public class WorkerServiceContext : DbContext
    {
        //public DbSet<> ActionQueueCollection { get; set; }

        public WorkerServiceContext() : base() { }
        public WorkerServiceContext(DbContextOptions<WorkerServiceContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<>().ToTable("");
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
