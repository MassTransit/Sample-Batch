using GreenPipes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Courier;
using Microsoft.Extensions.Logging;
using MassTransit.Courier.Exceptions;

namespace SampleBatch.Components.Activities.SuspendOrder
{
    public class SuspendOrderActivity : IExecuteActivity<SuspendOrderArguments>
    {
        private ILogger _logger;

        public SuspendOrderActivity(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SuspendOrderActivity>();
        }

        public async Task<ExecutionResult> Execute(ExecuteContext<SuspendOrderArguments> context)
        {
            _logger.LogInformation("Suspending {OrderId}", context.Arguments.OrderId);

            var random = new Random(DateTime.Now.Millisecond);

            if (random.Next(1, 10) == 1)
            {
                throw new RoutingSlipException("Order shipped, cannot suspend");
            }
            else
            {
                await Task.Delay(random.Next(1, 7) * 1000);
                return context.Completed();
            }
        }
    }
}
