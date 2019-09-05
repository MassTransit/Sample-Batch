using MassTransit;
using MassTransit.Courier;
using MassTransit.Courier.Contracts;
using MassTransit.Definition;
using Microsoft.Extensions.Logging;
using SampleBatch.Common;
using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SampleBatch.Components.Consumers
{
    public class ProcessBatchJobConsumer :
        IConsumer<ProcessBatchJob>
    {
        readonly ILogger<ProcessBatchJobConsumer> _log;

        public ProcessBatchJobConsumer(ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<ProcessBatchJobConsumer>();
        }

        public async Task Consume(ConsumeContext<ProcessBatchJob> context)
        {
            using (_log.BeginScope("ProcessBatchJob {BatchJobId}, {OrderId}", context.Message.BatchJobId, context.Message.OrderId))
            {
                var builder = new RoutingSlipBuilder(NewId.NextGuid());


                switch (context.Message.Action)
                {
                    case BatchAction.CancelOrders:
                        builder.AddActivity(
                            "CancelOrder",
                            context.GetDestinationAddress("cancel-order_execute"),
                            new
                            {
                                context.Message.OrderId,
                                Reason = "Product discontinued"
                            });

                        await builder.AddSubscription(
                            context.SourceAddress,
                            RoutingSlipEvents.ActivityFaulted,
                            RoutingSlipEventContents.None,
                            "CancelOrder",
                            x => x.Send<BatchJobFailed>(new
                            {
                                context.Message.BatchJobId,
                                context.Message.BatchId,
                                context.Message.OrderId
                            }));
                        break;
                    case BatchAction.SuspendOrders:
                        builder.AddActivity(
                            "SuspendOrder",
                            context.GetDestinationAddress("suspend-order_execute"),
                            new
                            {
                                context.Message.OrderId
                            });

                        await builder.AddSubscription(
                            context.SourceAddress,
                            RoutingSlipEvents.ActivityFaulted,
                            RoutingSlipEventContents.None,
                            "SuspendOrder",
                            x => x.Send<BatchJobFailed>(new
                            {
                                context.Message.BatchJobId,
                                context.Message.BatchId,
                                context.Message.OrderId
                            }));
                        break;
                    default:
                        break;
                }

                await builder.AddSubscription(
                    context.SourceAddress,
                    RoutingSlipEvents.Completed,
                    x => x.Send<BatchJobCompleted>(new
                    {
                        context.Message.BatchJobId,
                        context.Message.BatchId
                    }));

                await context.Execute(builder.Build());
            }
        }
    }
}
