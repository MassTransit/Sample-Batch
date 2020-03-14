namespace SampleBatch.Contracts
{
    using System;


    public interface BatchRejected
    {
        Guid BatchId { get; }
        DateTime Timestamp { get; }
        string Reason { get; }
    }
}