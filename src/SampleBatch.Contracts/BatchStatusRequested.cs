namespace SampleBatch.Contracts
{
    using System;


    public interface BatchStatusRequested
    {
        Guid BatchId { get; }

        DateTime Timestamp { get; }
    }
}