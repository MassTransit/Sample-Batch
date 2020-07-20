using Microsoft.EntityFrameworkCore;
using SampleBatch.Components.StateMachines;


namespace SampleBatch.Components
{
    public class SampleBatchDbContext : DbContext
    {
        public SampleBatchDbContext(DbContextOptions options)
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
