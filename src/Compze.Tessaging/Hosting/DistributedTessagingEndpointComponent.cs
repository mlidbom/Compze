using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting;

///<summary>The distributed Tessaging pipeline's runtime lifecycle within an endpoint: inbox and scheduler listen, the endpoint's<br/>
/// listening address is announced to every registered <see cref="IEndpointAddressAnnouncer"/>, the router connects to all endpoints,<br/>
/// the outbox sends.</summary>
sealed class DistributedTessagingEndpointComponent : IEndpointComponent, IAsyncDisposable
{
   readonly TommandScheduler _tommandScheduler;
   readonly IInbox _inbox;
   readonly IOutbox _outbox;
   readonly ITessagingRouter _tessagingRouter;
   readonly IEndpointRegistry _endpointRegistry;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;
   readonly EndpointConfiguration _configuration;
   readonly IReadOnlyList<IEndpointAddressAnnouncer> _addressAnnouncers;

   bool _isListening;

   internal DistributedTessagingEndpointComponent(IRootResolver resolver, IReadOnlyList<IEndpointAddressAnnouncer> addressAnnouncers)
   {
      _tommandScheduler = resolver.Resolve<TommandScheduler>();
      _inbox = resolver.Resolve<IInbox>();
      _outbox = resolver.Resolve<IOutbox>();
      _tessagingRouter = resolver.Resolve<ITessagingRouter>();
      _endpointRegistry = resolver.Resolve<IEndpointRegistry>();
      _backgroundExceptionReporter = resolver.Resolve<IBackgroundExceptionReporter>();
      _configuration = resolver.Resolve<EndpointConfiguration>();
      _addressAnnouncers = addressAnnouncers;
   }

   ///<summary>The address where the inbox listens; null until listening.</summary>
   internal EndpointAddress? Address => _isListening ? _inbox.Address : null;

   public async Task StartListeningAsync()
   {
      await Task.WhenAll(_inbox.StartAsync(), _tommandScheduler.StartAsync()).caf();
      _isListening = true;
      //Announcing is the final act of starting to listen, so an announced address is always one that is actually listening.
      _addressAnnouncers.ForEach(announcer => announcer.AnnounceEndpointAddress(_configuration.Id, _inbox.Address));
   }

   public async Task StartSendingAsync()
   {
      //The router converges on the registry's membership - including ourselves: scheduled tommands dispatch over the remote
      //protocol for the delivery guarantees - and keeps reconciling, so endpoints in other processes that appear, disappear,
      //or restart at a new address are connected, dropped, or re-connected as the registry changes.
      await _tessagingRouter.StartMaintainingConnectionsAsync(_endpointRegistry, _inbox.Address).caf();
      await _outbox.StartAsync().caf();
   }

   public async Task StopSendingAsync()
   {
      _tommandScheduler.Stop();
      await _outbox.StopAsync().caf();
   }

   public async Task StopListeningAsync()
   {
      //Retracting is the first act of stopping to listen: stop advertising the address before going deaf on it.
      _addressAnnouncers.ForEach(announcer => announcer.RetractEndpointAddress(_configuration.Id));
      _isListening = false;
      await _inbox.StopAsync().caf();
      _tessagingRouter.Stop();
   }

   public ValueTask DisposeAsync()
   {
      _tommandScheduler.Dispose();
      _backgroundExceptionReporter.ThrowIfAnyExceptions();
      return ValueTask.CompletedTask;
   }
}
