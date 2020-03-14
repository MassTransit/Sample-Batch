namespace SampleBatch.Contracts
{
    using System;


    public interface CancelBatch
    {
        Guid BatchId { get; }

        DateTime Timestamp { get; }
    }
}