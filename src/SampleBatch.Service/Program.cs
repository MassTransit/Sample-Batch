using MassTransit;
using MassTransit.Definition;
using MassTransit.EntityFrameworkCoreIntegration;
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
    using Microsoft.Extensions.DependencyInjection.Extensions;


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

                    services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
                    services.AddMassTransit(cfg =>
                    {
                        cfg.AddSagaStateMachine<BatchStateMachine, BatchState>(typeof(BatchStateMachineDefinition))
                            .EntityFrameworkRepository(r =>
                            {
                                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

                                r.AddDbContext<DbContext, SampleBatchDbContext>((provider, optionsBuilder) =>
                                {
                                    optionsBuilder.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch"));
                                });

                                // I specified the MsSqlLockStatements because in my State Entities EFCore EntityConfigurations, I changed the column name from CorrelationId, to "BatchId" and "BatchJobId"
                                // Otherwise I could just use r.UseSqlServer(), which uses the default, which are "... WHERE CorrelationId = @p0"
                                r.LockStatementProvider =
                                    new CustomSqlLockStatementProvider("select * from {0}.{1} WITH (UPDLOCK, ROWLOCK) WHERE BatchId = @p0");
                            });

                        cfg.AddSagaStateMachine<JobStateMachine, JobState>(typeof(JobStateMachineDefinition))
                            .EntityFrameworkRepository(r =>
                            {
                                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

                                r.AddDbContext<DbContext, SampleBatchDbContext>((provider, optionsBuilder) =>
                                {
                                    optionsBuilder.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch"));
                                });

                                // I specified the MsSqlLockStatements because in my State Entities EFCore EntityConfigurations, I changed the column name from CorrelationId, to "BatchId" and "BatchJobId"
                                // Otherwise I could just use r.UseSqlServer(), which uses the default, which are "... WHERE CorrelationId = @p0"
                                r.LockStatementProvider =
                                    new CustomSqlLockStatementProvider("select * from {0}.{1} WITH (UPDLOCK, ROWLOCK) WHERE BatchJobId = @p0");
                            });

                        cfg.AddConsumersFromNamespaceContaining<ConsumerAnchor>();
                        cfg.AddActivitiesFromNamespaceContaining<ActivitiesAnchor>();

                        cfg.AddBus(ConfigureBus);
                    });

                    services.AddDbContext<DbContext, SampleBatchDbContext>(x => x.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch")));

                    services.AddHostedService<MassTransitConsoleHostedService>();

                    // So we don't need to use ef migrations for this sample.
                    // Likely if you are going to deploy to a production environment, you want a better DB deploy strategy.
                    services.AddHostedService<EfDbCreatedHostedService>();

                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = "localhost";
                    });
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            if (isService)
            {
                await builder.UseWindowsService().Build().RunAsync();
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

            if (appSettings.RabbitMq != null)
            {
                return ConfigureRabbitMqBus(provider, appSettings);
            }

            throw new ApplicationException("Invalid Bus configuration. Couldn't find Azure or RabbitMq config");
        }

        static IBusControl ConfigureRabbitMqBus(IServiceProvider provider, AppConfig appConfig)
        {
            var endpointNameFormatter = provider.GetService<IEndpointNameFormatter>() ?? KebabCaseEndpointNameFormatter.Instance;

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(appConfig.RabbitMq.HostAddress, appConfig.RabbitMq.VirtualHost, h =>
                {
                    h.Username(appConfig.RabbitMq.Username);
                    h.Password(appConfig.RabbitMq.Password);
                });

                EndpointConvention.Map<ProcessBatchJob>(new Uri($"queue:{endpointNameFormatter.Consumer<ProcessBatchJobConsumer>()}"));

                cfg.UseInMemoryScheduler();

                cfg.ConfigureEndpoints(provider, endpointNameFormatter);
            });
        }

        static IBusControl ConfigureAzureSb(IServiceProvider provider, AppConfig appConfig)
        {
            var endpointNameFormatter = provider.GetService<IEndpointNameFormatter>() ?? KebabCaseEndpointNameFormatter.Instance;

            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Host(appConfig.AzureServiceBus.ConnectionString);

                EndpointConvention.Map<ProcessBatchJob>(new Uri($"queue:{endpointNameFormatter.Consumer<ProcessBatchJobConsumer>()}"));

                cfg.UseServiceBusMessageScheduler();

                cfg.ConfigureEndpoints(provider, endpointNameFormatter);
            });
        }
    }
}