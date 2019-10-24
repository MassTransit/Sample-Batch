using System;
using MassTransit;
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
            services.AddMvc();

            services.Configure<AppConfig>(options => Configuration.GetSection("AppConfig").Bind(options));

            services.AddMassTransit(cfg =>
            {
                cfg.AddBus(ConfigureBus);
                cfg.AddRequestClient<SubmitBatch>();
            });

            services.AddOpenApiDocument(cfg => cfg.PostProcess = d => d.Info.Title = "Sample-Batch");

            services.AddSingleton<IHostedService, MassTransitHostedService>();
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

                cfg.ConfigureEndpoints(provider);
            });
        }

        static IBusControl ConfigureAzureSb(IServiceProvider provider, AppConfig appConfig)
        {
            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                var host = cfg.Host(appConfig.AzureServiceBus.ConnectionString, h => { });

                cfg.ConfigureEndpoints(provider);
            });
        }
    }
}
