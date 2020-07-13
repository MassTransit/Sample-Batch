namespace SampleBatch.Contracts
{
    using System;
    using System.Collections.Generic;

    public interface BatchJobCompleted
    {
        Guid BatchJobId { get; }
        Guid BatchId { get; }
        DateTime Timestamp { get; }
        IDictionary<string, object> Variables { get; }
    }
}