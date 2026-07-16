using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Hosting;

///<summary>The exactly-once Tessaging pipeline's runtime lifecycle within an endpoint: the inbox listens, the outbox's<br/>
/// durable storage is initialized before any endpoint in the host starts sending — the sending phase's connection delivery streams<br/>
/// load their recovery backlogs from it — and the peer registry loads the endpoint's remembered peers. (The endpoint's one<br/>
/// transport server runs its own lifecycle in <see cref="Compze.Internals.Transport.EndpointTransportServerFeature"/>'s component;<br/>
/// the router's connection and delivery lifecycle belongs to the transport-speaking core's<br/>
/// <see cref="DistributedTessagingEndpointComponent"/>.)</summary>
sealed class ExactlyOnceTessagingEndpointComponent : IEndpointComponent
{
   readonly IInbox _inbox;
   readonly IOutbox _outbox;
   readonly IPeerRegistry _peerRegistry;

   internal ExactlyOnceTessagingEndpointComponent(IRootResolver resolver)
   {
      _inbox = resolver.Resolve<IInbox>();
      _outbox = resolver.Resolve<IOutbox>();
      _peerRegistry = resolver.ResolveSet<IPeerRegistry>().Single();
   }

   public async Task StartListeningAsync() => await Task.WhenAll(_inbox.StartAsync(), _outbox.StartAsync(), _peerRegistry.StartAsync()).caf();

   public Task StopSendingAsync() => _outbox.StopAsync();

   public Task StopListeningAsync() => Task.CompletedTask;
}
