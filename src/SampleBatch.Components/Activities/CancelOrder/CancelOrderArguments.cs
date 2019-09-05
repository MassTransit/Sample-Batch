using System;

namespace SampleBatch.Components.Activities.CancelOrder
{
    public interface CancelOrderArguments
    {
        Guid OrderId { get; }
        string Reason { get; }
    }
}
