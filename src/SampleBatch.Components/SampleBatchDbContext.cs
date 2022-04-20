namespace SampleBatch.Components
{
    using Microsoft.EntityFrameworkCore;
    using StateMachines;


    public class SampleBatchDbContext : DbContext
    {
        public SampleBatchDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<BatchState> BatchStates { get; set; }
        public DbSet<BatchJobState> JobStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BatchStateEntityConfiguration());
            modelBuilder.ApplyConfiguration(new JobStateEntityConfiguration());
        }
    }
}