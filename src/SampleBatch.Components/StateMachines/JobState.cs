using Automatonymous;
using SampleBatch.Contracts.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SampleBatch.Components.StateMachines
{
    public class JobState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public Guid BatchId { get; set; }

        public Guid OrderId { get; set; }

        public string CurrentState { get; set; }

        public DateTime? ReceiveTimestamp { get; set; }

        public DateTime? CreateTimestamp { get; set; }

        public DateTime? UpdateTimestamp { get; set; }

        public BatchAction Action { get; set; }

        public string ExceptionMessage { get; set; }

        // Navigation Properties
        public BatchState Batch { get; set; }
    }
}
