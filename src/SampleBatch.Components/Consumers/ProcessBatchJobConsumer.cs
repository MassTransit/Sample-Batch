namespace SampleBatch.Components.Consumers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Activities;
    using Contracts;
    using MassTransit;
    using Microsoft.Extensions.Logging;

    public class ProcessBatchJobConsumer :
        IConsumer<ProcessBatchJob>
    {
        readonly ILogger<ProcessBatchJobConsumer> _log;
        readonly IEnumerable<IRoutingSlipFactory> _factories;

        public ProcessBatchJobConsumer(ILoggerFactory loggerFactory, IEnumerable<IRoutingSlipFactory> factories)
        {
            _factories = factories;
            _log = loggerFactory.CreateLogger<ProcessBatchJobConsumer>();
        }

        public async Task Consume(ConsumeContext<ProcessBatchJob> context)
        {
            using (_log.BeginScope("ProcessBatchJob {BatchJobId}, {OrderId}", context.Message.BatchJobId, context.Message.OrderId))
            {
                foreach (var factory in _factories)
                {
                    if (factory.CanCreateSlipFrom(context))
                    {
                        var routingSlip = await factory.CreateSlip(context);

                        await context.Execute(routingSlip);
                    }
                }

                _log.LogError("Couldn't handle unknown batch action {action}", context.Message.Action);
            }
        }
    }
}