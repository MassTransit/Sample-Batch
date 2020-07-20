namespace SampleBatch.Contracts.Enums.Internal
{
    using System;
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;


    class SuspendOrdersEnum : BatchActionEnum
    {
        public SuspendOrdersEnum()
            : base(2, "Suspend Orders")
        {
        }

        protected override async Task SetupRoutingSlip(RoutingSlipBuilder builder, ConsumeContext<ProcessBatchJob> context)
        {
            builder.AddActivity(
                "SuspendOrder",
                new Uri("queue:suspend-order_execute"),
                new { context.Message.OrderId });

            await builder.AddSubscription(
                context.SourceAddress,
                RoutingSlipEvents.ActivityFaulted,
                RoutingSlipEventContents.None,
                "SuspendOrder",
                x => x.Send<BatchJobFailed>(new
                {
                    context.Message.BatchJobId,
                    context.Message.BatchId,
                    context.Message.OrderId
                }));
        }
    }
}
