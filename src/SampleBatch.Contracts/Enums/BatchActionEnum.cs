namespace SampleBatch.Contracts.Enums
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Converter;
    using Internal;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;
    using Newtonsoft.Json;


    [JsonConverter(typeof(BatchActionEnumConverter))]
    public abstract class BatchActionEnum
    {
        public static readonly BatchActionEnum CancelOrders = new CancelOrdersEnum();
        public static readonly BatchActionEnum SuspendOrders = new SuspendOrdersEnum();

        public int Value { get; private set; }
        public string Name { get; private set; }

        public static IEnumerable<BatchActionEnum> List()
        {
            yield return CancelOrders;
            yield return SuspendOrders;
        }
        
        protected BatchActionEnum(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public async Task<RoutingSlip> SetupRoutingSlip(ConsumeContext<ProcessBatchJob> context, Func<RoutingSlipBuilder, Task> commonAction)
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());

            await SetupRoutingSlip(builder, context);

            await commonAction?.Invoke(builder);

            return builder.Build();

        }

        protected abstract Task SetupRoutingSlip(RoutingSlipBuilder builder, ConsumeContext<ProcessBatchJob> context);

    }
}
