using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// An endpoint: a LocalTessagingEngine given identity and a wire — an <see cref="EndpointId"/> (stable identity across
/// restarts), one transport server with one address, discovery and announcement, the router, peer memory, and the TessageBus
/// delivery machinery of its tier. There are exactly two endpoint types, differing only in what crossing the endpoint
/// boundary guarantees: <see cref="BestEffortEndpoint"/> and <see cref="ExactlyOnceEndpoint"/>. Both serve all four tessage
/// kinds, unconditionally.
///
/// The endpoint is a plain composition root: it owns its container and drives its own lifecycle phases in the methods below —
/// listen → announce → send on the way up, retract → stop sending → stop listening on the way down. An
/// <see cref="IEndpointHost"/> runs each phase host-wide across its endpoints, so an announced address is always one that is
/// actually listening and nothing sends to an endpoint not yet ready to receive.
///</summary>
public abstract class Endpoint : IEndpoint
{
   readonly IDependencyInjectionContainer _container;
   readonly EndpointConfiguration _configuration;
   readonly IEndpointTransportServer _transportServer;
   readonly IReadOnlyList<IEndpointAddressAnnouncer> _addressAnnouncers;
   //Null when the endpoint declares no discovery registry: it serves whatever reaches it, and its own roster serves its sends inline, but it connects to no other endpoint.
   readonly IEndpointRegistry? _endpointRegistry;
   readonly ITessagingRouter _router;
   readonly IPeerRegistry _peerRegistry;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;
   readonly TeventObservationDispatcher _observationDispatcher;

   bool _isListening;
   bool _hasAnnounced;
   bool _isSending;

   private protected Endpoint(IDependencyInjectionContainer container,
                              EndpointConfiguration configuration,
                              IReadOnlyList<IEndpointAddressAnnouncer> addressAnnouncers,
                              IEndpointRegistry? endpointRegistry)
   {
      _container = container;
      _configuration = configuration;
      _addressAnnouncers = addressAnnouncers;
      _endpointRegistry = endpointRegistry;
      ServiceLocator = container.RootResolver;
      _transportServer = ServiceLocator.Resolve<IEndpointTransportServer>();
      _router = ServiceLocator.Resolve<ITessagingRouter>();
      _peerRegistry = ServiceLocator.Resolve<IPeerRegistry>();
      _backgroundExceptionReporter = ServiceLocator.Resolve<IBackgroundExceptionReporter>();
      _observationDispatcher = ServiceLocator.Resolve<TeventObservationDispatcher>();
   }

   ///<summary>The endpoint's stable identity: addresses are per-instance and change across restarts; the <see cref="EndpointId"/> never does.</summary>
   public EndpointId Id => _configuration.Id;

   public IRootResolver ServiceLocator { get; }

   ///<summary>The address where the endpoint's one transport server listens — serving every remotable capability the endpoint<br/>
   /// speaks, of every tessage kind. Null until the endpoint is listening.</summary>
   public EndpointAddress? Address => _isListening ? _transportServer.Address : null;

   public bool IsRunning => _isListening && _isSending;

   public async Task StartListeningAsync()
   {
      State.Assert(!_isListening);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting to listen");
      _isListening = true;

      await _peerRegistry.StartAsync().caf();
      await StartTheDurableVerticalAsync().caf();
      await _transportServer.StartAsync().caf();
   }

   public Task AnnounceAddressAsync()
   {
      State.Assert(_isListening && !_hasAnnounced);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) announcing address");
      _hasAnnounced = true;
      _addressAnnouncers.ForEach(announcer => announcer.AnnounceEndpointAddress(Id, _transportServer.Address));
      return Task.CompletedTask;
   }

   public async Task StartSendingAsync()
   {
      State.Assert(!_isSending);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting to send");
      _isSending = true;

      //The router converges on the registry's membership - minus the endpoint's own announced address: routes lead only to
      //other endpoints, since the roster serves in-roster tommands inline and the endpoint's own tevent subscriptions by
      //in-boundary participation. The transport server started in the listening phase, which the host completes everywhere
      //before any sending starts, so its address exists here - and so does every durable storage the exactly-once tier
      //initialized, which is what lets the connections' exactly-once streams load their recovery backlogs when delivery starts.
      await _router.StartMaintainingConnectionsAsync(_endpointRegistry, _transportServer.Address).caf();
      _router.StartDelivery();
   }

   public async Task StopSendingAsync()
   {
      if(!_isSending) return;
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping sending");
      _isSending = false;
      _router.StopDelivery();
      await StopTheDurableVerticalAsync().caf();
   }

   public Task RetractAddressAsync()
   {
      if(!_hasAnnounced) return Task.CompletedTask;
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) retracting address");
      _hasAnnounced = false;
      _addressAnnouncers.ForEach(announcer => announcer.RetractEndpointAddress(Id));
      return Task.CompletedTask;
   }

   public async Task StopListeningAsync()
   {
      if(!_isListening) return;
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping listening");
      _isListening = false;
      _router.Stop();
      await _transportServer.StopAsync().caf();
   }

   ///<summary>Starts the exactly-once tier's durable vertical in the listening phase — the best-effort endpoint has none, so the base does nothing.</summary>
   private protected virtual Task StartTheDurableVerticalAsync() => Task.CompletedTask;

   ///<summary>Stops the exactly-once tier's durable vertical when sending stops — the best-effort endpoint has none, so the base does nothing.</summary>
   private protected virtual Task StopTheDurableVerticalAsync() => Task.CompletedTask;

   public async ValueTask DisposeAsync()
   {
      this.Log().Debug($"Endpoint '{_configuration.Name}' ({Id}) disposing");
      await RetractAddressAsync().caf();
      await StopSendingAsync().caf();
      await StopListeningAsync().caf();
      //Drain the observation queues while the container still serves the observers their scopes: once container disposal
      //begins, scope creation is refused, so a drain left to the container's own singleton teardown could no longer run the
      //observers. Nothing enqueues new observation work after listening stopped - except observers themselves, which the
      //drain's passes cover.
      _observationDispatcher.Dispose();
      await _container.DisposeAsync().caf();
      //The container also disposes the server it holds; server disposal is idempotent, and disposing what we hold keeps ownership legible.
      await _transportServer.DisposeAsync().caf();
      _backgroundExceptionReporter.ThrowIfAnyExceptions();
   }
}
