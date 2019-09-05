using System;

namespace SampleBatch.Contracts
{
    public interface BatchJobDone
    {
        Guid BatchJobId { get; }

        Guid BatchId { get; }

        DateTime Timestamp { get; }
    }
}
