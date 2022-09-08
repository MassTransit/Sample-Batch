using FluentAssertions;
using MassTransit;
using MassTransit.Initializers;
using MassTransit.Saga;
using MassTransit.Testing;
using SampleBatch.Components.StateMachines;
using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SampleBatch.Tests.Unit
{
    /// <summary>
    /// The unit tests are all in memory. I prefer to be more thorough and test all transitions and branches of the State Machine in the unit tests.
    /// </summary>
    public class BatchStateMachineTests : IAsyncLifetime
    {
        private readonly InMemoryTestHarness _testHarness;
        private readonly InMemorySagaRepository<BatchState> _sagaRepository;
        private readonly BatchStateMachine _stateMachine;

        public BatchStateMachineTests()
        {
            _sagaRepository = new InMemorySagaRepository<BatchState>();
            _stateMachine = new BatchStateMachine();

            _testHarness = new InMemoryTestHarness();
            _testHarness.OnConfigureInMemoryReceiveEndpoint += ConfigureInMemoryReceiveEndpoint;
            _testHarness.OnConfigureInMemoryBus += ConfigureInMemoryBus;
        }

        public async Task InitializeAsync()
        {
            await _testHarness.Start();
        }

        public Task DisposeAsync()
        {
            return _testHarness.Stop();
        }

        private void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.StateMachineSaga(_stateMachine, _sagaRepository);
        }

        private void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.UseMessageScheduler(TestConstants.QuartzAddress);
        }

        [Fact]
        public async Task should_start()
        {
            var (message, _) = await MessageInitializerCache<BatchReceived>.InitializeMessage(
                new
                {
                    BatchId = NewId.NextGuid(),
                    Timestamp = DateTime.UtcNow,
                    Action = BatchAction.CancelOrders,
                    ActiveThreshold = 5,
                    OrderIds = new Guid[] { Guid.NewGuid(), Guid.NewGuid() }
                });

            await _testHarness.InputQueueSendEndpoint.Send(message);

            var sagaId = await _sagaRepository.ShouldContainSagaInState(message.BatchId, _stateMachine, x => x.Started, _testHarness.TestTimeout);

            sagaId.HasValue.Should().BeTrue();

            var saga = _sagaRepository[sagaId.Value];

            saga.Instance.CorrelationId.Should().Be(message.BatchId);
            saga.Instance.CreateTimestamp.Should().Be(message.Timestamp);
            saga.Instance.UpdateTimestamp.Should().Be(message.Timestamp);
            saga.Instance.Action.Should().Be(message.Action);
            saga.Instance.ProcessingOrderIds.Values.Should().BeEquivalentTo(message.OrderIds);
        }

        [Fact]
        public async Task should_receive_and_wait()
        {
            var (message, _) = await MessageInitializerCache<BatchReceived>.InitializeMessage(
                new
                {
                    BatchId = NewId.NextGuid(),
                    Timestamp = DateTime.UtcNow,
                    Action = BatchAction.CancelOrders,
                    ActiveThreshold = 5,
                    OrderIds = new Guid[] { Guid.NewGuid(), Guid.NewGuid() },
                    DelayInSeconds = 60
                });

            await _testHarness.InputQueueSendEndpoint.Send(message);

            var sagaId = await _sagaRepository.ShouldContainSagaInState(message.BatchId, _stateMachine, x => x.Received, _testHarness.TestTimeout);

            sagaId.HasValue.Should().BeTrue();

            var saga = _sagaRepository[sagaId.Value];

            saga.Instance.CorrelationId.Should().Be(message.BatchId);
            saga.Instance.CreateTimestamp.Should().Be(message.Timestamp);
            saga.Instance.UpdateTimestamp.Should().Be(message.Timestamp);
            saga.Instance.Action.Should().Be(message.Action);
            saga.Instance.UnprocessedOrderIds.Should().BeEquivalentTo(message.OrderIds);
            saga.Instance.ScheduledId.Should().NotBeNull();
        }
    }
}
