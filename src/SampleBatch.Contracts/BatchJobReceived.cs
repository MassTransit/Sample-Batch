using SampleBatch.Contracts.Enums;
using System;

namespace SampleBatch.Contracts
{
    public interface BatchJobReceived
    {
        Guid BatchJobId { get; }
        Guid BatchId { get; }
        Guid OrderId { get; }
        DateTime Timestamp { get; }
        BatchAction Action { get; }
    }
}
