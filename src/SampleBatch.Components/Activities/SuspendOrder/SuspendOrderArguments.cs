using System;

namespace SampleBatch.Components.Activities.SuspendOrder
{
    public interface SuspendOrderArguments
    {
        Guid OrderId { get; }
    }
}
