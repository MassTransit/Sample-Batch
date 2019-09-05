using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SampleBatch.Contracts.Enums;
using System;

namespace SampleBatch.Components.StateMachines
{
    class JobStateEntityConfiguration : IEntityTypeConfiguration<JobState>
    {
        public void Configure(EntityTypeBuilder<JobState> builder)
        {
            builder.HasKey(c => c.CorrelationId);

            builder.Property(c => c.CorrelationId)
                .ValueGeneratedNever()
                .HasColumnName("BatchJobId");

            builder.Property(c => c.CurrentState).IsRequired();

            builder.Property(c => c.Action)
                .HasConversion(new EnumToStringConverter<BatchAction>());
        }
    }
}
