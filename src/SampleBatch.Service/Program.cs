namespace SampleBatch.Service
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Components;
    using Components.Activities;
    using Components.Consumers;
    using Components.StateMachines;
    using Contracts;
    using MassTransit;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;


    class Program
    {
        public static AppConfig AppConfig { get; set; }

        static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var appConfig = hostContext.Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>();
                    services.Configure<AppConfig>(options => hostContext.Configuration.GetSection("AppConfig").Bind(options));

                    services.AddMassTransit(cfg =>
                    {
                        cfg.SetKebabCaseEndpointNameFormatter();
                        cfg.AddSagaStateMachine<BatchStateMachine, BatchState>(typeof(BatchStateMachineDefinition))
                            .EntityFrameworkRepository(r =>
                            {
                                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

                                r.AddDbContext<DbContext, SampleBatchDbContext>((provider, optionsBuilder) =>
                                {
                                    optionsBuilder.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch"));
                                });

                                // I specified the MsSqlLockStatements because in my State Entities EFCore EntityConfigurations, I changed the column name from CorrelationId, to "BatchId" and "BatchJobId"
                                // Otherwise, I could just use r.UseSqlServer(), which uses the default, which are "... WHERE CorrelationId = @p0"
                                r.LockStatementProvider =
                                    new CustomSqlLockStatementProvider();
                            });

                        cfg.AddSagaStateMachine<BatchJobStateMachine, BatchJobState, JobStateMachineDefinition>()
                            .EntityFrameworkRepository(r =>
                            {
                                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

                                r.AddDbContext<DbContext, SampleBatchDbContext>((provider, optionsBuilder) =>
                                {
                                    optionsBuilder.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch"));
                                });

                                // I specified the MsSqlLockStatements because in my State Entities EFCore EntityConfigurations, I changed the column name from CorrelationId, to "BatchId" and "BatchJobId"
                                // Otherwise, I could just use r.UseSqlServer(), which uses the default, which are "... WHERE CorrelationId = @p0"
                                r.LockStatementProvider =
                                    new CustomSqlLockStatementProvider();
                            });

                        cfg.AddConsumersFromNamespaceContaining<ConsumerAnchor>();
                        cfg.AddActivitiesFromNamespaceContaining<ActivitiesAnchor>();

                        if (appConfig.AzureServiceBus != null)
                        {
                            cfg.UsingAzureServiceBus((x, y) =>
                            {
                                y.Host(appConfig.AzureServiceBus.ConnectionString);

                                y.UseServiceBusMessageScheduler();

                                y.ConfigureEndpoints(x);
                            });
                        }
                        else if (appConfig.RabbitMq != null)
                        {
                            cfg.UsingRabbitMq((x, y) =>
                            {
                                y.Host(appConfig.RabbitMq.HostAddress, appConfig.RabbitMq.VirtualHost, h =>
                                {
                                    h.Username(appConfig.RabbitMq.Username);
                                    h.Password(appConfig.RabbitMq.Password);
                                });

                                y.UseInMemoryScheduler();

                                y.ConfigureEndpoints(x);
                            });
                        }
                        else
                            throw new ApplicationException("Invalid Bus configuration. Couldn't find Azure or RabbitMq config");
                    });

                    services.AddDbContext<SampleBatchDbContext>(x => x.UseSqlServer(hostContext.Configuration.GetConnectionString("sample-batch")));

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
                await builder.UseWindowsService().Build().RunAsync();
            else
                await builder.RunConsoleAsync();
        }
    }
}