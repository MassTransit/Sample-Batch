namespace SampleBatch.Contracts
{
    using System;
    using Enums;


    public interface BatchJobReceived
    {
        Guid BatchJobId { get; }
        Guid BatchId { get; }
        Guid OrderId { get; }
        DateTime Timestamp { get; }
        BatchActionEnum Action { get; }
    }
}