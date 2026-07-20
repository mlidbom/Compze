using Compze.Tessaging.TessageBus.Internal.BestEffortDelivery;
using Compze.Tessaging.Internal.TessagesInFlight;

namespace Compze.Tessaging.TessageBus.Internal.Outbox;

///<summary>The endpoint's durable send side: tevents travel its <see cref="IExactlyOnceTeventDeliveryLeg"/> face; exactly-once<br/>
/// tommands are sent through <see cref="SendTransactionallyAsync"/>.</summary>
interface IOutbox : IExactlyOnceTeventDeliveryLeg
{
    Task StartAsync();
    Task StopAsync();
    Task SendTransactionallyAsync(IExactlyOnceTommand exactlyOnceTommand);
}
