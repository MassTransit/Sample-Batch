namespace SampleBatch.Contracts
{
    using System;


    public interface BatchJobCompleted
    {
        Guid BatchJobId { get; }
        Guid BatchId { get; }
        DateTime Timestamp { get; }
        string Reason { get; }
    }
}