using System;

namespace SampleBatch.Contracts
{
    public interface BatchSubmitted
    {
        Guid BatchId { get; }

        DateTime Timestamp { get; }
    }
}
