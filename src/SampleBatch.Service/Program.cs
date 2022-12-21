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

                        cfg.SetEntityFrameworkSagaRepositoryProvider(r =>
                        {
                            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

                            r.ExistingDbContext<SampleBatchDbContext>();

                            r.UseSqlServer();
                        });

                        cfg.AddConsumersFromNamespaceContaining<ConsumerAnchor>();
                        cfg.AddActivitiesFromNamespaceContaining<ActivitiesAnchor>();
                        cfg.AddSagasFromNamespaceContaining<StateMachineAnchor>();
                        cfg.AddSagaStateMachinesFromNamespaceContaining<StateMachineAnchor>();

                        if (appConfig.AzureServiceBus != null)
                        {
                            cfg.AddServiceBusMessageScheduler();
                            cfg.UsingAzureServiceBus((x, y) =>
                            {
                                y.UseServiceBusMessageScheduler();
                                
                                y.Host(appConfig.AzureServiceBus.ConnectionString);

                                y.ConfigureEndpoints(x);
                            });
                        }
                        else if (appConfig.RabbitMq != null)
                        {
                            cfg.AddDelayedMessageScheduler();
                            cfg.UsingRabbitMq((x, y) =>
                            {
                                y.UseDelayedMessageScheduler();

                                y.Host(appConfig.RabbitMq.HostAddress, appConfig.RabbitMq.VirtualHost, h =>
                                {
                                    h.Username(appConfig.RabbitMq.Username);
                                    h.Password(appConfig.RabbitMq.Password);
                                });

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