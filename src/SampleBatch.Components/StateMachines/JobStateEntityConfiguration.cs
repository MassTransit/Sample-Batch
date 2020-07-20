namespace SampleBatch.Components.StateMachines
{
    using System.Linq;
    using Contracts.Enums;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;


    class JobStateEntityConfiguration :
        IEntityTypeConfiguration<JobState>
    {
        public void Configure(EntityTypeBuilder<JobState> builder)
        {
            builder.HasKey(c => c.CorrelationId);

            builder.Property(c => c.CorrelationId)
                .ValueGeneratedNever()
                .HasColumnName("BatchJobId");

            builder.Property(c => c.CurrentState).IsRequired();

            builder.Property(p => p.Action)
                .HasConversion(v => v.Value, i => BatchActionEnum.List().FirstOrDefault(e => e.Value == i));
        }
    }
}
