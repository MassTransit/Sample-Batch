namespace SampleBatch.Components.Activities
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Courier.Contracts;

    public interface IRoutingSlipFactory
    {
        bool CanCreateSlipFrom(ConsumeContext<ProcessBatchJob> context);
        Task<RoutingSlip> CreateSlip(ConsumeContext<ProcessBatchJob> context);
    }
}