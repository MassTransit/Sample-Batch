namespace SampleBatch.Components.StateMachines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Contracts.Enums;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;


    class BatchStateEntityConfiguration :
        IEntityTypeConfiguration<BatchState>
    {
        public void Configure(EntityTypeBuilder<BatchState> builder)
        {
            builder.HasKey(c => c.CorrelationId);

            builder.Property(c => c.CorrelationId)
                .ValueGeneratedNever()
                .HasColumnName("BatchId");

            builder.Property(c => c.CurrentState).IsRequired();

            builder.Property(p => p.Action)
                .HasConversion(v => v.Value, i => BatchActionEnum.List().FirstOrDefault(e => e.Value == i));


            builder.Property(c => c.UnprocessedOrderIds)
                .HasConversion(new JsonValueConverter<Stack<Guid>>())
                .Metadata.SetValueComparer(new JsonValueComparer<Stack<Guid>>());

            builder.Property(c => c.ProcessingOrderIds)
                .HasConversion(new JsonValueConverter<Dictionary<Guid, Guid>>())
                .Metadata.SetValueComparer(new JsonValueComparer<Dictionary<Guid, Guid>>());
        }
    }
}
