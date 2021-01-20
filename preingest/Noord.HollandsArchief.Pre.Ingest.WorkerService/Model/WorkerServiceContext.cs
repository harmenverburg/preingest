using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.Context;


namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Model
{
    public class WorkerServiceContext : DbContext
    {
        public DbSet<ActionQueue> ActionQueueCollection { get; set; }

        public WorkerServiceContext() : base() { }
        public WorkerServiceContext(DbContextOptions<WorkerServiceContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActionQueue>().ToTable("ActionQueue");
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
