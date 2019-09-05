using SampleBatch.Contracts.Enums;
using System;

namespace SampleBatch.Contracts
{
    public interface CancelBatch
    {
        Guid BatchId { get; set; }

        DateTime Timestamp { get; set; }
    }
}
