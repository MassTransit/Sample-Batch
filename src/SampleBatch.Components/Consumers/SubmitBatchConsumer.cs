using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;
using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SampleBatch.Components.Consumers
{
    public class SubmitBatchConsumer :
        IConsumer<SubmitBatch>
    {
        readonly ILogger<SubmitBatchConsumer> _log;

        public SubmitBatchConsumer(ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<SubmitBatchConsumer>();
        }

        public async Task Consume(ConsumeContext<SubmitBatch> context)
        {
            using (_log.BeginScope("SubmitBatch {BatchId}", context.Message.BatchId))
            {
                if (_log.IsEnabled(LogLevel.Debug))
                    _log.LogDebug("Validating batch {BatchId}", context.Message.BatchId);

                // do some validation
                if(context.Message.OrderIds.Length == 0)
                {
                    await context.RespondAsync<BatchRejected>(new
                    {
                        context.Message.BatchId,
                        ReceiveTime = DateTime.UtcNow,
                        Reason = "Must have at least one OrderId to Process"
                    });

                    return;
                }

                await context.Publish(new Received(context.Message.BatchId, context.Message.Action, context.Message.OrderIds, context.Message.ActiveThreshold, context.Message.DelayInSeconds));

                await context.RespondAsync<BatchSubmitted>(new
                {
                    context.Message.BatchId,
                    ReceiveTime = DateTime.UtcNow,
                });

                if (_log.IsEnabled(LogLevel.Debug))
                    _log.LogDebug("Accepted order {BatchId}", context.Message.BatchId);
            }
        }

        class Received : BatchReceived
        {
            public Received(Guid batchId, BatchAction action, Guid[] orderIds, int activeThreshold, int? delayInSeconds = null)
            {
                BatchId = batchId;
                Timestamp = DateTime.UtcNow;
                Action = action;
                OrderIds = orderIds;
                ActiveThreshold = activeThreshold;
                DelayInSeconds = delayInSeconds;
            }

            public Guid BatchId { get; }
            public DateTime Timestamp { get; }
            public BatchAction Action { get; }
            public Guid[] OrderIds { get; } = Array.Empty<Guid>();
            public int ActiveThreshold { get; }
            public int? DelayInSeconds { get; }
        }
    }


    //public class SubmitBatchJobConsumerDefinition :
    //    ConsumerDefinition<SubmitBatchJobConsumer>
    //{
    //    public SubmitBatchJobConsumerDefinition()
    //    {
    //        ConcurrentMessageLimit = 10;
    //    }
    //}
}
