using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SampleBatch.Service
{
    public class SagaScopedDbConnectionFactory<TSaga> :
    ISagaDbContextFactory<TSaga>
    where TSaga : class, ISaga
    {
        public SagaScopedDbConnectionFactory()
        {
        }

        public DbContext Create()
        {
            throw new Exception("should never call this");
        }

        public DbContext CreateScoped<T>(ConsumeContext<T> context)
            where T : class
        {
            if (context.TryGetPayload(out IServiceScope currentScope))
                return currentScope.ServiceProvider.GetRequiredService<DbContext>();

            return Create();
        }

        public void Release(DbContext dbContext)
        {
            // Purposely left blank, the disposal of the dbContext is controlled by the container (autofac in this case)
        }
    }
}
