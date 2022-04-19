using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration.Saga;
using MassTransit.Initializers;
using MassTransit.Saga;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using SampleBatch.Components;
using SampleBatch.Components.StateMachines;
using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SampleBatch.Tests.Integration
{
    using MassTransit.EntityFrameworkCoreIntegration;


    /// <summary>
    /// Integration Tests I like to test more end to end scenarios. This still uses in-memory for all the message broker bits, but instead it uses MsSql for the persistence
    /// </summary>
    public class BatchStateMachineTests : IAsyncLifetime
    {
        private readonly Func<SampleBatchDbContext> _dbContextFactory;

        private readonly ISagaRepository<BatchState> _sagaRepository;
        private readonly BatchStateMachine _stateMachine;

        private readonly InMemoryTestHarness _inMemoryTestHarness;
        private readonly ConsumerTestHarness<FakeBatchJobSagaConsumer> _inMemoryConsumerHarness;
        

        public BatchStateMachineTests()
        {
            var dbOptionsBuilder = new DbContextOptionsBuilder()
                .UseSqlServer(TestConstants.ConnectionString)
                .EnableSensitiveDataLogging();

            _dbContextFactory = () => new SampleBatchDbContext(dbOptionsBuilder.Options);

            // Makes sure the DB is created for tests
            using(var db = _dbContextFactory())
            {
                db.Database.EnsureCreated();
            }

            _sagaRepository = EntityFrameworkSagaRepository<BatchState>.CreatePessimistic(_dbContextFactory, new CustomSqlLockStatementProvider("select * from {0}.{1} WITH (UPDLOCK, ROWLOCK) WHERE BatchId = @p0"));
            _stateMachine = new BatchStateMachine();

            _inMemoryTestHarness = new InMemoryTestHarness();
            _inMemoryConsumerHarness = _inMemoryTestHarness.Consumer<FakeBatchJobSagaConsumer>(Guid.NewGuid().ToString());
            _inMemoryTestHarness.OnConfigureInMemoryReceiveEndpoint += ConfigureInMemoryReceiveEndpoint;
            _inMemoryTestHarness.OnConfigureInMemoryBus += ConfigureInMemoryBus;
        }

        public async Task InitializeAsync()
        {
            await _inMemoryTestHarness.Start();
        }

        public Task DisposeAsync()
        {
            return _inMemoryTestHarness.Stop();
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
        public async Task should_complete_successfully()
        {
            var (message, _) = await MessageInitializerCache<BatchReceived>.InitializeMessage(
                new
                {
                    BatchId = NewId.NextGuid(),
                    Timestamp = DateTime.UtcNow,
                    BatchAction = BatchAction.CancelOrders,
                    ActiveThreshold = 5,
                    OrderIds = new Guid[] { Guid.NewGuid(), Guid.NewGuid() }
                });

            await _inMemoryTestHarness.InputQueueSendEndpoint.Send(message);

            var sagaId = await _sagaRepository.ShouldContainSaga(x => x.CorrelationId == message.BatchId && x.CurrentState == _stateMachine.Started.Name, _inMemoryTestHarness.TestTimeout);

            using (var dbContext = _dbContextFactory())
            {
                var saga = await dbContext.BatchStates.SingleOrDefaultAsync(x => x.CorrelationId == sagaId.Value);
                saga.ProcessingOrderIds.Values.Should().BeEquivalentTo(message.OrderIds);
            }

            var job1 = _inMemoryConsumerHarness.Consumed.Select<BatchJobReceived>(x => x.Context.Message.OrderId == message.OrderIds[0]).First();
            var job2 = _inMemoryConsumerHarness.Consumed.Select<BatchJobReceived>(x => x.Context.Message.OrderId == message.OrderIds[1]).First();

            var doneJob1 = await MessageInitializerCache<BatchJobDone>.InitializeMessage(
                new
                {
                    job1.Context.Message.BatchJobId,
                    job1.Context.Message.BatchId,
                    Timestamp = DateTime.UtcNow
                });
            var doneJob2 = await MessageInitializerCache<BatchJobDone>.InitializeMessage(
                new
                {
                    job2.Context.Message.BatchJobId,
                    job2.Context.Message.BatchId,
                    Timestamp = DateTime.UtcNow
                });

            await _inMemoryTestHarness.Bus.Publish(doneJob1);
            await _inMemoryTestHarness.Bus.Publish(doneJob2);

            sagaId = await _sagaRepository.ShouldContainSaga(x => x.CorrelationId == message.BatchId && x.CurrentState == _stateMachine.Finished.Name, _inMemoryTestHarness.TestTimeout);

            using (var dbContext = _dbContextFactory())
            {
                var saga = await dbContext.BatchStates.SingleOrDefaultAsync(x => x.CorrelationId == sagaId.Value);
                saga.ProcessingOrderIds.Should().BeEmpty();
                saga.UnprocessedOrderIds.Should().BeEmpty();
            }
        }

        private class FakeBatchJobSagaConsumer
            : IConsumer<BatchJobReceived>
        {
            public Task Consume(ConsumeContext<BatchJobReceived> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
