using System;

namespace SampleBatch.Contracts
{
    public interface BatchRejected
    {
        Guid BatchId { get; }

        DateTime Timestamp { get; }

        string Reason { get; set; }
    }
}
