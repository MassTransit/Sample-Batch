using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;
using System;

namespace SampleBatch.Api.Models
{
    public class CreateBatch : SubmitBatch
    {
        public Guid BatchId { get; set; }
        public DateTime Timestamp { get; set; }
        public BatchAction Action { get; set; }
        public Guid[] OrderIds { get; set; } = Array.Empty<Guid>();
        public int ActiveThreshold { get; set; }
        public int? DelayInSeconds { get; set; }
    }
}
