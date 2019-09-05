using Automatonymous;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using SampleBatch.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SampleBatch.Components.StateMachines
{
    public class JobStateMachine : MassTransitStateMachine<JobState>
    {
        public JobStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => BatchJobReceived, x => x.CorrelateById(c => c.Message.BatchJobId));
            Event(() => BatchJobCompleted, x => x.CorrelateById(c => c.Message.BatchJobId));
            Event(() => BatchJobFailed, x => x.CorrelateById(c => c.Message.BatchJobId));

            Initially(
                When(BatchJobReceived)
                    .Then(context => Touch(context.Instance, context.Data.Timestamp))
                    .Then(context => SetReceiveTimestamp(context.Instance, context.Data.Timestamp))
                    .Then(Initialize)
                    .ThenAsync(InitiateProcessing)
                    .TransitionTo(Received));

            During(Received,
                When(BatchJobCompleted)
                    .Then(context => Touch(context.Instance, context.Data.Timestamp))
                    .Publish(context => new JobDone { BatchJobId = context.Instance.CorrelationId, BatchId = context.Instance.BatchId, Timestamp = DateTime.UtcNow })
                    .TransitionTo(Completed),
                When(BatchJobFailed)
                    .Then(context => Touch(context.Instance, context.Data.Timestamp))
                    .Then(context => context.Instance.ExceptionMessage = context.Data.ExceptionInfo.Message)
                    .Publish(context => new JobDone { BatchJobId = context.Instance.CorrelationId, BatchId = context.Instance.BatchId, Timestamp = DateTime.UtcNow })
                    .TransitionTo(Failed));
        }

        public State Received { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }

        public Event<BatchJobReceived> BatchJobReceived { get; private set; }
        public Event<BatchJobFailed> BatchJobFailed { get; private set; }
        public Event<BatchJobCompleted> BatchJobCompleted { get; private set; }

        private static void Touch(JobState state, DateTime timestamp)
        {
            if (!state.CreateTimestamp.HasValue)
                state.CreateTimestamp = timestamp;

            if (!state.UpdateTimestamp.HasValue || state.UpdateTimestamp.Value < timestamp)
                state.UpdateTimestamp = timestamp;
        }

        private static void SetReceiveTimestamp(JobState state, DateTime timestamp)
        {
            if (!state.ReceiveTimestamp.HasValue || state.ReceiveTimestamp.Value > timestamp)
                state.ReceiveTimestamp = timestamp;
        }

        private static void Initialize(BehaviorContext<JobState, BatchJobReceived> context)
        {
            InitializeInstance(context.Instance, context.Data);
        }

        private static void InitializeInstance(JobState instance, BatchJobReceived data)
        {
            instance.Action = data.Action;
            instance.OrderId = data.OrderId;
            instance.BatchId = data.BatchId;
        }

        private static async Task InitiateProcessing(BehaviorContext<JobState, BatchJobReceived> context)
        {
            await context.Send<JobState, BatchJobReceived, ProcessBatchJob>(new { BatchJobId = context.Instance.CorrelationId, Timestamp = DateTime.UtcNow, BatchId = context.Instance.BatchId, OrderId = context.Instance.OrderId, context.Instance.Action });
        }

        class JobDone : BatchJobDone
        {
            public Guid BatchJobId { get; set; }

            public Guid BatchId { get; set; }

            public DateTime Timestamp { get; set; }
        }
    }

    public class JobStateMachineDefinition :
        SagaDefinition<JobState>
    {
        public JobStateMachineDefinition()
        {
            ConcurrentMessageLimit = 8;
        }

        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<JobState> sagaConfigurator)
        {

            sagaConfigurator.UseMessageRetry(r => r.Immediate(5));
            //sagaConfigurator.UseInMemoryOutbox();
        }
    }
}
