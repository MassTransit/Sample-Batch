using System;
using System.Threading.Tasks;
using MassTransit.Courier;
using Microsoft.Extensions.Logging;
using MassTransit.Courier.Exceptions;

namespace SampleBatch.Components.Activities.CancelOrder
{
    public class CancelOrderActivity : ExecuteActivity<CancelOrderArguments>
    {
        private ILogger _logger;

        public CancelOrderActivity(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CancelOrderActivity>();
        }

        public async Task<ExecutionResult> Execute(ExecuteContext<CancelOrderArguments> context)
        {
            _logger.LogInformation("Cancelling {OrderId}, with Reason: {Reason}", context.Arguments.OrderId, context.Arguments.Reason);

            var random = new Random(DateTime.Now.Millisecond);

            if (random.Next(1, 10) == 1)
            {
                throw new RoutingSlipException("Order shipped, cannot cancel");
            }
            else
            {
                await Task.Delay(random.Next(1, 7) * 1000);
                return context.Completed();
            }
        }
    }
}
