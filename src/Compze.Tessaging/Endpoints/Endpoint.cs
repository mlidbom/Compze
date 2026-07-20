using Compze.Tessaging.Endpoints.Discovery;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Internals.HandlerAvailability;
using Compze.Tessaging.Internals.Peers;
using Compze.Tessaging.Internals.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Transport;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// An endpoint: a LocalTessagingEngine given identity and a wire — an <see cref="EndpointId"/> (stable identity across
/// restarts), one transport server with one address, discovery and announcement, the router, peer memory, and the TessageBus
/// delivery machinery of its tier. There are exactly two endpoint types, differing only in what crossing the endpoint
/// boundary guarantees: <see cref="BestEffortEndpoint"/> and <see cref="ExactlyOnceEndpoint"/>. Both serve all four tessage
/// kinds, unconditionally.
///
/// The endpoint is first-class: a plain composition root that owns its container and drives its own lifecycle —
/// <see cref="StartAsync"/> runs the up-phases in order (listen → announce → send), disposal runs the mirror
/// (retract → stop sending → stop listening), so an announced address is always one that is actually listening. The
/// individual phase methods remain public for callers that script a phase deliberately — a specification stopping the
/// listening to observe delivery waiting out downtime, a late-started endpoint. An <see cref="IEndpointHost"/> is an
/// optional convenience starting and disposing several endpoints together; it adds nothing an endpoint cannot do alone.
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
   readonly IHandlerAvailability _handlerAvailability;
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
      _handlerAvailability = ServiceLocator.Resolve<IHandlerAvailability>();
      _backgroundExceptionReporter = ServiceLocator.Resolve<IBackgroundExceptionReporter>();
      _observationDispatcher = ServiceLocator.Resolve<TeventObservationDispatcher>();
   }

   ///<summary>The endpoint's stable identity: addresses are per-instance and change across restarts; the <see cref="EndpointId"/> never does.</summary>
   internal EndpointId Id => _configuration.Id;

   public IRootResolver ServiceLocator { get; }

   ///<summary>The address where the endpoint's one transport server listens — serving every remotable capability the endpoint<br/>
   /// speaks, of every tessage kind. Null until the endpoint is listening, and again once it stops: the transport server clears<br/>
   /// its address before tearing down, so this is safe to read concurrently with the endpoint starting or stopping.</summary>
   public EndpointAddress? Address => _transportServer.Address;

   public bool IsRunning => _isListening && _isSending;

   public async Task StartAsync()
   {
      await StartListeningAsync().caf();
      await AnnounceAddressAsync().caf();
      await StartSendingAsync().caf();
   }

   async Task StartListeningAsync()
   {
      State.Assert(!_isListening);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting to listen");

      //Whether this process may run the endpoint at all is decided before the endpoint touches anything else: a refused claim
      //throws out of here with the endpoint still fully un-started, so its teardown has nothing to unwind.
      await ClaimTheProcessLeaseAsync().caf();
      _isListening = true;

      await _peerRegistry.StartAsync().caf();
      await StartTheDurableVerticalAsync().caf();
      await _transportServer.StartAsync().caf();
   }

   Task AnnounceAddressAsync()
   {
      State.Assert(_isListening && !_hasAnnounced);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) announcing address");
      _hasAnnounced = true;
      var address = _transportServer.Address._assert().NotNull(); //The listening phase already ran, so the server has an address.
      _addressAnnouncers.ForEach(announcer => announcer.AnnounceEndpointAddress(Id, address));
      return Task.CompletedTask;
   }

   async Task StartSendingAsync()
   {
      State.Assert(!_isSending);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting to send");
      _isSending = true;

      //The router converges on the registry's membership - minus the endpoint's own announced address: routes lead only to
      //other endpoints, since the roster serves in-roster tommands inline and the endpoint's own tevent subscriptions by
      //in-boundary participation. The endpoint's own listening phase already ran: the transport server's address exists here,
      //and so does every durable storage the exactly-once tier initialized - which is what lets the connections' exactly-once
      //streams load their recovery backlogs when delivery starts.
      await _router.StartMaintainingConnectionsAsync(_endpointRegistry, _transportServer.Address._assert().NotNull()).caf();
      _router.StartDelivery();
   }

   async Task StopSendingAsync()
   {
      if(!_isSending) return;
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping sending");
      _isSending = false;
      _router.StopDelivery();
      await StopTheDurableVerticalAsync().caf();
   }

   Task RetractAddressAsync()
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

   public async Task AwaitReadinessAsync(ReadinessTypes readinessTypes, TimeSpan? patience = null)
   {
      //Asserted rather than waited for: readiness measures the world outside the endpoint, and an endpoint that has not
      //started cannot see that world - its router discovers nothing before the sending phase - so awaiting readiness on it
      //would time out with a message blaming the deployment for what is a startup-ordering mistake here.
      State.Assert(IsRunning, () => "Readiness is awaited on a started endpoint: start the endpoint (or the host that owns it) first.");
      await _handlerAvailability.AwaitHandlersForAsync(readinessTypes, patience).caf();
   }

   ///<summary>The exactly-once tier registers the endpoint in the domain database's endpoint catalog and claims its process<br/>
   /// lease — the first act of starting to listen, before anything else touches the database. The best-effort endpoint has no<br/>
   /// database, so the base does nothing.</summary>
   private protected virtual Task ClaimTheProcessLeaseAsync() => Task.CompletedTask;

   ///<summary>The exactly-once tier releases its process lease at disposal — after the observation drain, once nothing in<br/>
   /// this process writes to the domain database anymore. The best-effort endpoint has no database, so the base does nothing.</summary>
   private protected virtual Task ReleaseTheProcessLeaseAsync() => Task.CompletedTask;

   ///<summary>Starts the exactly-once tier's durable vertical in the listening phase — the best-effort endpoint has none, so the base does nothing.</summary>
   private protected virtual Task StartTheDurableVerticalAsync() => Task.CompletedTask;

   ///<summary>Stops the exactly-once tier's durable vertical when sending stops — the best-effort endpoint has none, so the base does nothing.</summary>
   private protected virtual Task StopTheDurableVerticalAsync() => Task.CompletedTask;

   ///<summary>Drains the exactly-once inbox before teardown: waits for every already-received tessage to finish handling so the<br/>
   /// endpoint tears down with empty queues. The best-effort endpoint has no inbox, so the base does nothing.</summary>
   private protected virtual Task DrainTheInboxAsync() => Task.CompletedTask;

   public async ValueTask DisposeAsync()
   {
      this.Log().Debug($"Endpoint '{_configuration.Name}' ({Id}) disposing");
      try
      {
         await RetractAddressAsync().caf();
         await StopSendingAsync().caf();
         await StopListeningAsync().caf();
         //After listening stops nothing new arrives, so the inbox drains to empty here - while the container still serves its
         //handlers their scopes - before teardown. Empty queues before teardown is the graceful-shutdown property (nothing
         //half-processed) that a rolling restart or a serialization-format change wants.
         await DrainTheInboxAsync().caf();
         //Drain the observation queues while the container still serves the observers their scopes: once container disposal
         //begins, scope creation is refused, so a drain left to the container's own singleton teardown could no longer run the
         //observers. Nothing enqueues new observation work after listening stopped - except observers themselves, which the
         //drain's passes cover.
         _observationDispatcher.Dispose();
         //After the drain: nothing in this process writes to the domain database anymore, so the process lease can be freed for
         //the endpoint's next process.
         await ReleaseTheProcessLeaseAsync().caf();
      }
      finally
      {
         //Resource release must run even when a quiescing step above throws - most easily a delivery thread that will not stop,
         //whose 5s join throws out of StopSendingAsync. Disposing the container joins the router's reconcile loop, ending the
         //peer-registry transaction a pass may hold open, and disposes every other singleton. Skipping it abandons that
         //transaction - and its connection, released only by transaction completion and held by a process-wide static pool -
         //open until the transaction-timeout backstop fires. The quiescing exception still propagates once the finally completes.
         await _container.DisposeAsync().caf();
         //The container also disposes the server it holds; server disposal is idempotent, and disposing what we hold keeps ownership legible.
         await _transportServer.DisposeAsync().caf();
      }
      _backgroundExceptionReporter.ThrowIfAnyExceptions();
   }
}
