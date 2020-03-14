namespace SampleBatch.Contracts
{
    using System;


    public interface BatchNotFound
    {
        Guid BatchId { get; }

        DateTime Timestamp { get; }
    }
}