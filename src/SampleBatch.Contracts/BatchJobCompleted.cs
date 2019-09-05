using System;

namespace SampleBatch.Contracts
{
    public interface BatchJobCompleted
    {
        Guid BatchJobId { get; }
        Guid BatchId { get; }
        DateTime Timestamp { get; }
    }
}
