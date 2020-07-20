﻿namespace SampleBatch.Contracts
{
    using System;
    using Enums;


    public interface BatchReceived
    {
        Guid BatchId { get; }
        DateTime Timestamp { get; }
        BatchActionEnum Action { get; }
        Guid[] OrderIds { get; }
        int ActiveThreshold { get; }

        int? DelayInSeconds { get; }
    }
}