namespace SampleBatch.Components.Activities.CancelOrder
{
    using System;


    public interface CancelOrderArguments
    {
        Guid OrderId { get; }
        string Reason { get; }
    }
}