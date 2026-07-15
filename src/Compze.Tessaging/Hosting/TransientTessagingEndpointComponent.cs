using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting;

///<summary>The transport-speaking Tessaging core's runtime lifecycle within an endpoint: the router connects to all endpoints and<br/>
/// runs the connections' delivery streams. (The endpoint's one transport server — which serves arriving tessages and announces the<br/>
/// endpoint's address — runs its own lifecycle in <see cref="EndpointTransportServerFeature"/>'s component; the exactly-once<br/>
/// pipeline, when composed, runs its own in <see cref="ExactlyOnceTessagingEndpointComponent"/>.)</summary>
sealed class TransientTessagingEndpointComponent : IEndpointComponent, IAsyncDisposable
{
   readonly ITessagingRouter _tessagingRouter;
   //Null when the endpoint declares no discovery registry: it serves, converses in-process, and self-sends, but connects to no other endpoint.
   readonly IEndpointRegistry? _endpointRegistry;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;
   readonly EndpointTransportServerFeature _transportServer;

   internal TransientTessagingEndpointComponent(IRootResolver resolver, EndpointTransportServerFeature transportServer, IEndpointRegistry? endpointRegistry)
   {
      _tessagingRouter = resolver.Resolve<ITessagingRouter>();
      _endpointRegistry = endpointRegistry;
      _backgroundExceptionReporter = resolver.Resolve<IBackgroundExceptionReporter>();
      _transportServer = transportServer;
   }

   ///<summary>The endpoint's one listening address (see <see cref="EndpointTransportServerFeature.ListeningAddress"/>); null until the endpoint's transport server is listening.</summary>
   internal EndpointAddress? Address => _transportServer.ListeningAddress;

   public Task StartListeningAsync() => Task.CompletedTask;

   public async Task StartSendingAsync()
   {
      //The router converges on the registry's membership - plus the endpoint's own inbox always: an exactly-once tommand routes
      //to whichever endpoint advertises its type, the sender itself included, so self-sent tommands ride the outbox -> own-inbox
      //pipeline like any other - and keeps reconciling, so endpoints in other processes that appear, disappear, or restart at a
      //new address are connected, dropped, or re-connected as the registry changes.
      //The endpoint's transport server started in the listening phase, which the host completes everywhere before any sending
      //starts, so its address exists here - and so does every durable storage a composed exactly-once pipeline initialized,
      //which is what lets the connections' exactly-once streams load their recovery backlogs when delivery starts below.
      await _tessagingRouter.StartMaintainingConnectionsAsync(_endpointRegistry, _transportServer.ListeningAddress._assert().NotNull()).caf();
      _tessagingRouter.StartDelivery();
   }

   public Task StopSendingAsync()
   {
      _tessagingRouter.StopDelivery();
      return Task.CompletedTask;
   }

   public Task StopListeningAsync()
   {
      _tessagingRouter.Stop();
      return Task.CompletedTask;
   }

   public ValueTask DisposeAsync()
   {
      _backgroundExceptionReporter.ThrowIfAnyExceptions();
      return ValueTask.CompletedTask;
   }
}
