using SampleBatch.Contracts.Enums;
using System;

namespace SampleBatch.Contracts
{
    public interface SubmitBatch
    {
        Guid BatchId { get; set; }

        DateTime Timestamp { get; set; }

        BatchAction Action { get; set; }

        Guid[] OrderIds { get; set; }

        int ActiveThreshold { get; set; }

        int? DelayInSeconds { get; set; }
    }
}
