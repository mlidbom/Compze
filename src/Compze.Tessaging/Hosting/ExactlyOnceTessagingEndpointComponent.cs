using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Hosting;

///<summary>The exactly-once Tessaging pipeline's runtime lifecycle within an endpoint: the inbox listens, and the outbox's<br/>
/// durable storage is initialized before any endpoint in the host starts sending — the sending phase's connection delivery streams<br/>
/// load their recovery backlogs from it. (The endpoint's one transport server runs its own lifecycle in<br/>
/// <see cref="Compze.Internals.Transport.EndpointTransportServerFeature"/>'s component; the router's connection and delivery<br/>
/// lifecycle belongs to the transport-speaking core's <see cref="TransientTessagingEndpointComponent"/>.)</summary>
sealed class ExactlyOnceTessagingEndpointComponent : IEndpointComponent
{
   readonly IInbox _inbox;
   readonly IOutbox _outbox;

   internal ExactlyOnceTessagingEndpointComponent(IRootResolver resolver)
   {
      _inbox = resolver.Resolve<IInbox>();
      _outbox = resolver.Resolve<IOutbox>();
   }

   public async Task StartListeningAsync() => await Task.WhenAll(_inbox.StartAsync(), _outbox.StartAsync()).caf();

   public Task StopSendingAsync() => _outbox.StopAsync();

   public Task StopListeningAsync() => Task.CompletedTask;
}
