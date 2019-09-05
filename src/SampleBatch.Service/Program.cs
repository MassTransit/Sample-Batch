using MassTransit;
using MassTransit.Definition;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.EntityFrameworkCoreIntegration.Saga;
using MassTransit.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleBatch.Components;
using SampleBatch.Components.Consumers;
using SampleBatch.Components.StateMachines;
using SampleBatch.Contracts;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SampleBatch.Common;
using SampleBatch.Components.Activities;
using MassTransit.Azure.ServiceBus.Core;

namespace SampleBatch.Service
{
    class Program
    {
        public static AppConfig AppConfig { get; set; }

        static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AppConfig>(options => hostContext.Configuration.GetSection("AppConfig").Bind(options));

                    services.AddMassTransit(cfg =>
                    {
                        cfg.AddConsumersFromNamespaceContaining<ConsumerAnchor>();
                        cfg.AddSagaStateMachinesFromNamespaceContaining<StateMachineAnchor>();
                        cfg.AddActivitiesFromNamespaceContaining<ActivitiesAnchor>();
                        cfg.AddBus(ConfigureBus);
                    });

                    services.AddDbContext<DbContext, SampleBatchDbContext>(x => x.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch")));

                    services.AddSingleton(typeof(ISagaDbContextFactory<BatchState>), typeof(SagaScopedDbConnectionFactory<BatchState>));
                    services.AddSingleton(typeof(ISagaDbContextFactory<JobState>), typeof(SagaScopedDbConnectionFactory<JobState>));
                    
                    // I specified the MsSqlLockStatements because in my State Entities EFCore EntityConfigurations, I changed the column name from CorrelationId, to "BatchId" and "BatchJobId"
                    // Otherwise I could just use the default, which are "... WHERE CorrelationId = @p0"
                    services.AddSingleton<ISagaRepository<BatchState>>(x => EntityFrameworkSagaRepository<BatchState>.CreatePessimistic(x.GetRequiredService<ISagaDbContextFactory<BatchState>>(), new MsSqlLockStatements(rowLockStatement: "select * from {0}.{1} WITH (UPDLOCK, ROWLOCK) WHERE BatchId = @p0")));
                    services.AddSingleton<ISagaRepository<JobState>>(x => EntityFrameworkSagaRepository<JobState>.CreatePessimistic(x.GetRequiredService<ISagaDbContextFactory<JobState>>(), new MsSqlLockStatements(rowLockStatement: "select * from {0}.{1} WITH (UPDLOCK, ROWLOCK) WHERE BatchJobId = @p0")));

                    services.AddSingleton<IHostedService, MassTransitConsoleHostedService>();
                    services.AddSingleton<IHostedService, EfDbCreatedHostedService>(); // So we don't need to use ef migrations for this sample. Likely if you are going to deploy to a production environment, you want a better DB deploy strategy.
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            if (isService)
            {
                await builder.RunAsServiceAsync();
            }
            else
            {
                await builder.RunConsoleAsync();
            }
        }

        static IBusControl ConfigureBus(IServiceProvider provider)
        {
            var appSettings = provider.GetRequiredService<IOptions<AppConfig>>().Value;

            if (appSettings.AzureServiceBus != null)
            {
                return ConfigureAzureSb(provider, appSettings);
            }
            else if (appSettings.RabbitMq != null)
            {
                return ConfigureRabbitMqBus(provider, appSettings);
            }

            throw new ApplicationException("Invalid Bus configuration. Couldn't find Azure or RabbitMq config");
        }

        static IBusControl ConfigureRabbitMqBus(IServiceProvider provider, AppConfig appConfig)
        {
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(appConfig.RabbitMq.HostAddress, appConfig.RabbitMq.VirtualHost, h =>
                {
                    h.Username(appConfig.RabbitMq.Username);
                    h.Password(appConfig.RabbitMq.Password);
                });

                var endpointNameFormatter = new KebabCaseEndpointNameFormatter();

                EndpointConvention.Map<ProcessBatchJob>(host.Settings.HostAddress.GetDestinationAddress(endpointNameFormatter.Consumer<ProcessBatchJobConsumer>()));

                cfg.UseInMemoryScheduler();

                cfg.ConfigureEndpoints(provider, endpointNameFormatter);
            });
        }

        static IBusControl ConfigureAzureSb(IServiceProvider provider, AppConfig appConfig)
        {
            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                var host = cfg.Host(appConfig.AzureServiceBus.ConnectionString, h => { });

                var endpointNameFormatter = new KebabCaseEndpointNameFormatter();

                EndpointConvention.Map<ProcessBatchJob>(host.Settings.ServiceUri.GetDestinationAddress(endpointNameFormatter.Consumer<ProcessBatchJobConsumer>()));

                cfg.UseServiceBusMessageScheduler();

                cfg.ConfigureEndpoints(provider, endpointNameFormatter);
            });
        }
    }
}
