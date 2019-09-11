using MassTransit;
using MassTransit.Logging.Tracing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace SampleBatch.Api
{
    public class MassTransitHostedService :
        IHostedService
    {
        readonly IBusControl _bus;

        public MassTransitHostedService(IBusControl bus, ILoggerFactory loggerFactory)
        {
            _bus = bus;

            if (loggerFactory != null && MassTransit.Logging.Logger.Current.GetType() == typeof(TraceLogger))
                MassTransit.ExtensionsLoggingIntegration.ExtensionsLogger.Use(loggerFactory);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bus.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _bus.StopAsync(cancellationToken);
        }
    }
}
