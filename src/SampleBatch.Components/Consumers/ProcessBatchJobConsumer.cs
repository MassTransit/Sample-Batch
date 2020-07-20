namespace SampleBatch.Components.Consumers
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;
    using Microsoft.Extensions.Logging;


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
                var routingSlip = await context.Message.Action.SetupRoutingSlip(context, async builder =>
                {
                    await builder.AddSubscription(
                        context.SourceAddress,
                        RoutingSlipEvents.Completed,
                        x => x.Send<BatchJobCompleted>(new
                        {
                            context.Message.BatchJobId,
                            context.Message.BatchId,
                            InVar.Timestamp
                        }));
                });

                await context.Execute(routingSlip);
            }
        }
    }
}
