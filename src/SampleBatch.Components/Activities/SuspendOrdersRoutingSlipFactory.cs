namespace SampleBatch.Components.Activities;

using System;
using System.Threading.Tasks;
using Contracts;
using Contracts.Enums;
using MassTransit;
using MassTransit.Courier.Contracts;

public sealed class SuspendOrdersRoutingSlipFactory : IRoutingSlipFactory
{
    public bool CanCreateSlipFrom(ConsumeContext<ProcessBatchJob> context)
    {
        return context.Message.Action == BatchAction.SuspendOrders;
    }

    public async Task<RoutingSlip> CreateSlip(ConsumeContext<ProcessBatchJob> context)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());

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

        await builder.AddSubscription(
            context.SourceAddress,
            RoutingSlipEvents.Completed,
            x => x.Send<BatchJobCompleted>(new
            {
                context.Message.BatchJobId,
                context.Message.BatchId
            }));

        return builder.Build();
    }
}