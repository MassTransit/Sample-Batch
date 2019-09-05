using Microsoft.EntityFrameworkCore;
using SampleBatch.Components.StateMachines;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleBatch.Components
{
    public class SampleBatchDbContext : DbContext
    {
        public SampleBatchDbContext(DbContextOptions<SampleBatchDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BatchStateEntityConfiguration());
            modelBuilder.ApplyConfiguration(new JobStateEntityConfiguration());
        }

        public DbSet<BatchState> BatchStates { get; set; }
        public DbSet<JobState> JobStates { get; set; }
    }
}
