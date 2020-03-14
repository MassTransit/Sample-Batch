using System;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using MassTransit.Azure.ServiceBus.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SampleBatch.Contracts;


namespace SampleBatch.Api
{
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;


    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddMvc();

            services.Configure<AppConfig>(options => Configuration.GetSection("AppConfig").Bind(options));

            services.AddMassTransit(ConfigureBus, cfg =>
            {
                cfg.AddRequestClient<SubmitBatch>();
                cfg.AddRequestClient<BatchStatusRequested>();
            });

            services.AddOpenApiDocument(cfg => cfg.PostProcess = d => d.Info.Title = "Sample-Batch");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseOpenApi(); // serve OpenAPI/Swagger documents
            app.UseSwaggerUi3(); // serve Swagger UI

            app.UseHealthChecks("/health", new HealthCheckOptions {Predicate = check => check.Tags.Contains("ready")});

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
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
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(appConfig.RabbitMq.HostAddress, appConfig.RabbitMq.VirtualHost, h =>
                {
                    h.Username(appConfig.RabbitMq.Username);
                    h.Password(appConfig.RabbitMq.Password);
                });

                cfg.ConfigureEndpoints(provider);
            });
        }

        static IBusControl ConfigureAzureSb(IServiceProvider provider, AppConfig appConfig)
        {
            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Host(appConfig.AzureServiceBus.ConnectionString);

                cfg.ConfigureEndpoints(provider);
            });
        }
    }
}