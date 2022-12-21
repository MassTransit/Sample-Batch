namespace SampleBatch.Api
{
    using System;
    using System.Globalization;
    using System.Text;
    using Contracts;
    using MassTransit;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;


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

            var appConfig = Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>();
            services.Configure<AppConfig>(options => Configuration.GetSection("AppConfig").Bind(options));

            services.AddMassTransit(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();
                cfg.AddRequestClient<SubmitBatch>();
                cfg.AddRequestClient<BatchStatusRequested>();

                if (appConfig.AzureServiceBus != null)
                {
                    cfg.UsingAzureServiceBus((x, y) =>
                    {
                        y.Host(appConfig.AzureServiceBus.ConnectionString);
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

                        y.ConfigureEndpoints(x);
                    });
                }
                else
                    throw new ApplicationException("Invalid Bus configuration. Couldn't find Azure or RabbitMq config");
            });

            services.AddOpenApiDocument(cfg => cfg.PostProcess = d => d.Info.Title = "Sample-Batch");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseOpenApi(); // serve OpenAPI/Swagger documents
            app.UseSwaggerUi3(); // serve Swagger UI

            app.UseHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}