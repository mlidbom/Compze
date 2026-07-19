using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

///<summary>The endpoint's durable send side: tevents travel its <see cref="IExactlyOnceTeventDeliveryLeg"/> face; exactly-once<br/>
/// tommands are sent through <see cref="SendTransactionallyAsync"/>.</summary>
interface IOutbox : IExactlyOnceTeventDeliveryLeg
{
    Task StartAsync();
    Task StopAsync();
    Task SendTransactionallyAsync(IExactlyOnceTommand exactlyOnceTommand);
}