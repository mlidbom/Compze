using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting;

///<summary>The distributed Tessaging pipeline's runtime lifecycle within an endpoint: inbox and scheduler listen (the endpoint's<br/>
/// one transport server — which serves Tessaging's arriving tessages and announces the endpoint's address — runs its own lifecycle in<br/>
/// <see cref="Compze.Internals.Transport.EndpointTransportServerFeature"/>'s component), the router connects to all endpoints, the outbox sends.</summary>
sealed class DistributedTessagingEndpointComponent : IEndpointComponent, IAsyncDisposable
{
   readonly TommandScheduler _tommandScheduler;
   readonly IInbox _inbox;
   readonly IOutbox _outbox;
   readonly ITessagingRouter _tessagingRouter;
   readonly IEndpointRegistry _endpointRegistry;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;
   readonly EndpointTransportServerFeature _transportServer;

   internal DistributedTessagingEndpointComponent(IRootResolver resolver, EndpointTransportServerFeature transportServer)
   {
      _tommandScheduler = resolver.Resolve<TommandScheduler>();
      _inbox = resolver.Resolve<IInbox>();
      _outbox = resolver.Resolve<IOutbox>();
      _tessagingRouter = resolver.Resolve<ITessagingRouter>();
      _endpointRegistry = resolver.Resolve<IEndpointRegistry>();
      _backgroundExceptionReporter = resolver.Resolve<IBackgroundExceptionReporter>();
      _transportServer = transportServer;
   }

   ///<summary>The endpoint's one listening address (see <see cref="EndpointTransportServerFeature.ListeningAddress"/>); null until the endpoint's transport server is listening.</summary>
   internal EndpointAddress? Address => _transportServer.ListeningAddress;

   public async Task StartListeningAsync() => await Task.WhenAll(_inbox.StartAsync(), _tommandScheduler.StartAsync()).caf();

   public async Task StartSendingAsync()
   {
      //The router converges on the registry's membership - including ourselves: scheduled tommands dispatch over the remote
      //protocol for the delivery guarantees - and keeps reconciling, so endpoints in other processes that appear, disappear,
      //or restart at a new address are connected, dropped, or re-connected as the registry changes.
      //The endpoint's transport server started in the listening phase, which the host completes everywhere before any sending starts, so its address exists here.
      await _tessagingRouter.StartMaintainingConnectionsAsync(_endpointRegistry, _transportServer.ListeningAddress._assert().NotNull()).caf();
      await _outbox.StartAsync().caf();
   }

   public async Task StopSendingAsync()
   {
      _tommandScheduler.Stop();
      await _outbox.StopAsync().caf();
   }

   public Task StopListeningAsync()
   {
      _tessagingRouter.Stop();
      return Task.CompletedTask;
   }

   public ValueTask DisposeAsync()
   {
      _tommandScheduler.Dispose();
      _backgroundExceptionReporter.ThrowIfAnyExceptions();
      return ValueTask.CompletedTask;
   }
}
