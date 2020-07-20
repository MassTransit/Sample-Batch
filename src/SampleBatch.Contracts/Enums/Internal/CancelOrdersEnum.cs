namespace SampleBatch.Contracts.Enums.Internal
{
    using System;
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;


    class CancelOrdersEnum : BatchActionEnum
    {
        public CancelOrdersEnum()
            : base(1, "Cancel Orders")
        {
        }

        protected override async Task SetupRoutingSlip( RoutingSlipBuilder builder, ConsumeContext<ProcessBatchJob> context)
        {
            builder.AddActivity(
                "CancelOrder",
                new Uri("queue:cancel-order_execute"),
                new
                {
                    context.Message.OrderId,
                    Reason = "Product discontinued"
                });

            await builder.AddSubscription(
                context.SourceAddress,
                RoutingSlipEvents.ActivityFaulted,
                RoutingSlipEventContents.None,
                "CancelOrder",
                x => x.Send<BatchJobFailed>(new
                {
                    context.Message.BatchJobId,
                    context.Message.BatchId,
                    context.Message.OrderId
                }));
        }
    }
}
