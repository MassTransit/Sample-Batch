namespace SampleBatch.Components.StateMachines
{
    using System;
    using Contracts;
    using MassTransit;


    public class BatchJobStateMachine :
        MassTransitStateMachine<BatchJobState>
    {
        public BatchJobStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => BatchJobReceived, x => x.CorrelateById(c => c.Message.BatchJobId));
            Event(() => BatchJobCompleted, x => x.CorrelateById(c => c.Message.BatchJobId));
            Event(() => BatchJobFailed, x => x.CorrelateById(c => c.Message.BatchJobId));

            Initially(
                When(BatchJobReceived)
                    .Then(context => Touch(context.Saga, context.Message.Timestamp))
                    .Then(context => SetReceiveTimestamp(context.Saga, context.Message.Timestamp))
                    .Then(Initialize)
                    .PublishAsync(context => context.Init<ProcessBatchJob>(new
                    {
                        BatchJobId = context.Saga.CorrelationId,
                        InVar.Timestamp,
                        context.Saga.BatchId,
                        context.Saga.OrderId,
                        context.Saga.Action
                    }))
                    .TransitionTo(Received));

            During(Received,
                When(BatchJobCompleted)
                    .Then(context => Touch(context.Saga, context.Message.Timestamp))
                    .PublishAsync(context => context.Init<BatchJobDone>(new
                    {
                        BatchJobId = context.Saga.CorrelationId,
                        context.Saga.BatchId,
                        InVar.Timestamp
                    }))
                    .TransitionTo(Completed),
                When(BatchJobFailed)
                    .Then(context => Touch(context.Saga, context.Message.Timestamp))
                    .Then(context => context.Saga.ExceptionMessage = context.Message.ExceptionInfo.Message)
                    .PublishAsync(context => context.Init<BatchJobDone>(new
                    {
                        BatchJobId = context.Saga.CorrelationId,
                        context.Saga.BatchId,
                        InVar.Timestamp
                    }))
                    .TransitionTo(Failed));
        }

        public State Received { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }

        public Event<BatchJobReceived> BatchJobReceived { get; private set; }
        public Event<BatchJobFailed> BatchJobFailed { get; private set; }
        public Event<BatchJobCompleted> BatchJobCompleted { get; private set; }

        static void Touch(BatchJobState state, DateTime timestamp)
        {
            state.CreateTimestamp ??= timestamp;

            if (!state.UpdateTimestamp.HasValue || state.UpdateTimestamp.Value < timestamp)
                state.UpdateTimestamp = timestamp;
        }

        static void SetReceiveTimestamp(BatchJobState state, DateTime timestamp)
        {
            if (!state.ReceiveTimestamp.HasValue || state.ReceiveTimestamp.Value > timestamp)
                state.ReceiveTimestamp = timestamp;
        }

        static void Initialize(BehaviorContext<BatchJobState, BatchJobReceived> context)
        {
            InitializeInstance(context.Saga, context.Message);
        }

        static void InitializeInstance(BatchJobState instance, BatchJobReceived data)
        {
            instance.Action = data.Action;
            instance.OrderId = data.OrderId;
            instance.BatchId = data.BatchId;
        }
    }


    public class JobStateMachineDefinition :
        SagaDefinition<BatchJobState>
    {
        public JobStateMachineDefinition()
        {
            ConcurrentMessageLimit = 8;
        }

        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<BatchJobState> sagaConfigurator)
        {
            sagaConfigurator.UseMessageRetry(r => r.Immediate(5));
            sagaConfigurator.UseInMemoryOutbox();
        }
    }
}