using SampleBatch.Contracts.Enums;
using System;

namespace SampleBatch.Contracts
{
    public interface BatchReceived
    {
        Guid BatchId { get; }
        DateTime Timestamp { get; }
        BatchAction Action { get; }
        Guid[] OrderIds { get; }
        int ActiveThreshold { get; }

        int? DelayInSeconds { get; }
    }
}
