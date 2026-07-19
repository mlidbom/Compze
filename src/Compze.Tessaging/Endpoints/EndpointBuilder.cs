using Compze.Abstractions.Public;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.BestEffortDelivery;
using Compze.Tessaging.Implementation.HandlerAvailability;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.Transport.Client.Implementation;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Serialization;
using Compze.Tessaging.Transport;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Typermedia.Hosting;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// The declaration surface an endpoint is composed through — what <see cref="BestEffortEndpoint.Build"/> /
/// <see cref="ExactlyOnceEndpoint.Build"/> hand their composition callback. An endpoint is an engine given identity and a
/// wire, and this surface declares all three: the engine (<see cref="RegisterTessageHandlers"/>,
/// <see cref="ObserveTevents"/> — the same declaration block a plain container's LocalTessagingEngine uses, so an
/// application's handler declarations run unchanged under any composition), the identity (<see cref="Configuration"/>, given
/// at composition), and the wire: the transport protocol (<see cref="TransportProtocol"/>), the endpoint's one serializer
/// (<see cref="Serializer"/>), and the topology declarations (<see cref="ParticipateIn{TRegistry}"/> /
/// <see cref="DiscoverEndpointsThrough"/> / <see cref="AnnounceAddressTo"/>, <see cref="RequirePeers"/>,
/// <see cref="DoNotQueueTeventsFor"/>). Composition choices are parameters, not plugins: the implementation packages offer
/// their choices as extension methods over this surface (e.g. <c>NamedPipeEndpointTransport()</c>,
/// <c>NewtonsoftSerializer()</c>, <c>SqliteDomainDatabase(...)</c>), each filling the one parameter it names.
///
/// The builder exists only inside the composition callback: the callback's end is the declaration's end, the build closes
/// the roster, and any attempt to declare afterward explodes. Domain components register through <see cref="Registrar"/>,
/// and store integrations (a tevent store's <c>HandleTaggregate</c>, a document db's <c>HandleDocumentType</c>) plug into
/// this same surface — handler contributors like any other.
///
/// Type-id mappings are not declared here: a component declares the assemblies whose type identity it needs where it is
/// registered, through <c>Registrar.RequireMappedTypesFromAssemblyContaining&lt;T&gt;()</c>, and the endpoint's container
/// composes one <see cref="ITypeMap"/> from every such declaration.
///
/// Every declaration returns the builder as its concrete tier type (<typeparamref name="TConcreteBuilder"/>), so a
/// composition chains declaration after declaration in one fluent expression, and a tier-specific declaration stays
/// reachable after a base declaration instead of the chain narrowing to the base type mid-way.
///</summary>
///<typeparam name="TConcreteBuilder">The concrete tier builder subtype — the curiously-recurring self type — so every<br/>
/// inherited declaration returns that concrete type and a chain never loses the tier's own declarations.</typeparam>
public abstract class EndpointBuilder<TConcreteBuilder> where TConcreteBuilder : EndpointBuilder<TConcreteBuilder>
{
   readonly IContainerBuilder _containerBuilder;
   readonly LocalTessagingEngineBuilder _engine = new();

   readonly List<IEndpointAddressAnnouncer> _addressAnnouncers = [];
   readonly List<EndpointId> _requiredPeers = [];
   readonly List<EndpointId> _peersNotQueuedFor = [];
   HandlerAvailabilityPatience _handlerAvailabilityPatience = Implementation.HandlerAvailability.HandlerAvailabilityPatience.Default;
   bool _handlerAvailabilityPatienceDeclared;
   Action<IComponentRegistrar>? _registerTransportProtocol;
   Action<IComponentRegistrar>? _registerSerializer;
   ITessagesInFlightTracker? _tessagesInFlightTracker;
   bool _composed;

   private protected EndpointBuilder(IContainerBuilder containerBuilder, EndpointConfiguration configuration)
   {
      _containerBuilder = containerBuilder;
      Configuration = configuration;
   }

   ///<summary>This builder as its concrete tier type (<see cref="BestEffortEndpointBuilder"/> / <see cref="ExactlyOnceEndpointBuilder"/>) —<br/>
   /// the fluent return every declaration hands back, so a composition chains declaration after declaration without ever losing<br/>
   /// the tier's own declarations. The cast is the curiously-recurring generic's guarantee: the self type <em>is</em> this instance.</summary>
   TConcreteBuilder Self => (TConcreteBuilder)this;

   ///<summary>Registers the endpoint's components with its container — domain components, query models, whatever the endpoint's application code needs.</summary>
   public IComponentRegistrar Registrar => _containerBuilder.Registrar;

   ///<summary>The chainable form of <see cref="Registrar"/>: registers components without breaking out of the declaration<br/>
   /// chain — the same shape as <see cref="TransportProtocol"/> and <see cref="Serializer"/>. This is where an endpoint's<br/>
   /// components declare the type identity they need, through<br/>
   /// <c>RegisterComponents(registrar =&gt; registrar.RequireMappedTypesFromAssemblyContaining&lt;T&gt;())</c>.</summary>
   public TConcreteBuilder RegisterComponents(Action<IComponentRegistrar> register)
   {
      AssertStillComposing();
      register(Registrar);
      return Self;
   }

   ///<summary>The endpoint's identity and naming; also registered in the container so the endpoint's services can know which endpoint they serve.</summary>
   public EndpointConfiguration Configuration { get; }

   ///<summary>Declares handlers for all four tessage kinds through the one <see cref="TessageHandlerRegistrar"/> — see<br/>
   /// <see cref="LocalTessagingEngineBuilder.RegisterTessageHandlers"/>, whose declaration idiom this is.</summary>
   public TConcreteBuilder RegisterTessageHandlers(Action<TessageHandlerRegistrar> register)
   {
      AssertStillComposing();
      _engine.RegisterTessageHandlers(register);
      return Self;
   }

   ///<summary>Declares tevent observers — observation, the deliberately transaction-ignoring watch surface, under its own verb<br/>
   /// so the distinct semantics are visible at the declaration site — see <see cref="LocalTessagingEngineBuilder.ObserveTevents"/>.</summary>
   public TConcreteBuilder ObserveTevents(Action<TeventObservationRegistrar> registerObservations)
   {
      AssertStillComposing();
      _engine.ObserveTevents(registerObservations);
      return Self;
   }

   //todo: This just taking an IComponentRegistrar feels iffy. Can we make setting up the protocol easier and safer to do?
   ///<summary>Declares the endpoint's transport protocol — the strategy behind the endpoint's one transport server and its<br/>
   /// transport client. Declared exactly once, through a protocol package's named extension<br/>
   /// (e.g. <c>NamedPipeEndpointTransport()</c>, <c>AspNetCoreEndpointTransport()</c>).</summary>
   public TConcreteBuilder TransportProtocol(Action<IComponentRegistrar> registerProtocol)
   {
      AssertStillComposing();
      State.Assert(_registerTransportProtocol is null, () => "The endpoint already declared its transport protocol — an endpoint speaks exactly one.");
      _registerTransportProtocol = registerProtocol;
      return Self;
   }

   //todo: This just taking an IComponentRegistrar feels iffy. Can we make setting up serialization easier and safer to do?
   ///<summary>Declares the endpoint's one serializer — the format of everything the endpoint sends and receives, of every<br/>
   /// tessage kind. Declared at most once, through a serializer package's named extension (e.g. <c>NewtonsoftSerializer()</c>);<br/>
   /// a composition whose container already carries the serializers (a testing container cloned from a suite root) declares none.</summary>
   public TConcreteBuilder Serializer(Action<IComponentRegistrar> registerSerializer)
   {
      AssertStillComposing();
      State.Assert(_registerSerializer is null, () => "The endpoint already declared its serializer — an endpoint has exactly one.");
      _registerSerializer = registerSerializer;
      return Self;
   }

   ///<summary>Declares the registry through which this endpoint discovers the endpoints it converses with — the read side of<br/>
   /// discovery, whose write side is <see cref="AnnounceAddressTo"/>. The endpoint's router keeps reconciling its connections<br/>
   /// against the registry's membership. Declaring none means the endpoint discovers nothing: it serves whatever reaches it,<br/>
   /// and its own roster serves its sends inline (an in-roster tommand executes in the sender's execution, needing no<br/>
   /// discovery and no wire) — but it connects to no other endpoint.</summary>
   public TConcreteBuilder DiscoverEndpointsThrough(IEndpointRegistry registry)
   {
      AssertStillComposing();
      if(ReferenceEquals(EndpointRegistry, registry)) return Self; //Idempotent: ParticipateIn and a direct declaration may both name the endpoint's one registry.
      State.Assert(EndpointRegistry is null, () => $"The endpoint already declared the registry it discovers endpoints through — an endpoint discovers through exactly one {nameof(IEndpointRegistry)}.");
      EndpointRegistry = registry;
      return Self;
   }

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/>. The announcement is made<br/>
   /// in the endpoint's announcing phase — after its listening phase and before its sending phase — so an announced address<br/>
   /// is always one that is actually listening; it is retracted in the mirror phase, before the endpoint's sending stops, so<br/>
   /// the address stops being advertised before anything goes deaf. An endpoint<br/>
   /// announces to every announcer declared; declaring none means the endpoint is found some other way (a fixed address<br/>
   /// list) or only serves.</summary>
   public TConcreteBuilder AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      AssertStillComposing();
      if(!_addressAnnouncers.Contains(announcer)) _addressAnnouncers.Add(announcer); //Idempotent for the same announcer: ParticipateIn and a direct declaration may both name it.
      return Self;
   }

   ///<summary>Declares that the endpoint participates in <paramref name="registry"/>: it discovers the other endpoints through<br/>
   /// it (<see cref="DiscoverEndpointsThrough"/>) AND announces its own listening address to it (<see cref="AnnounceAddressTo"/>) —<br/>
   /// the composition a same-machine application suite uses, where every process both finds the others and is found by them.<br/>
   /// Declare the two sides separately instead when a deployment is asymmetric.</summary>
   public TConcreteBuilder ParticipateIn<TRegistry>(TRegistry registry) where TRegistry : IEndpointRegistry, IEndpointAddressAnnouncer
   {
      DiscoverEndpointsThrough(registry);
      AnnounceAddressTo(registry);
      return Self;
   }

   ///<summary>Declares peers this endpoint requires — peers it works with whose <see cref="EndpointId"/> the composition knows.<br/>
   /// The declaration is the durable peer memory a database-less endpoint cannot keep anywhere else: it survives restarts by<br/>
   /// being code. Every tevent published before a required peer's first advertisement is held for it — in order, within the<br/>
   /// queue bound — and the subset matching its subscriptions delivers when it is first met, so startup ordering stops<br/>
   /// mattering: nothing a required peer should see is lost to the discovery race<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
   public TConcreteBuilder RequirePeers(params EndpointId[] requiredPeers)
   {
      AssertStillComposing();
      State.Assert(!requiredPeers.Contains(Configuration.Id), () => "An endpoint cannot require itself: a peer is another endpoint, and the endpoint's own roster serves its tessages in-boundary.");
      State.Assert(!requiredPeers.Intersect(_peersNotQueuedFor).Any(), () => "A peer cannot be both required and not-queued-for: requiring a peer means holding everything for it until it is met, declining to queue means keeping nothing for it while it is away.");
      _requiredPeers.AddRange(requiredPeers);
      return Self;
   }

   ///<summary>Declares peers this endpoint deliberately keeps nothing for — the per-peer opt-down from queue-while-down<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>). Ephemerality is a property of the relationship, not of the endpoint:<br/>
   /// an application that requires its billing peer may genuinely not care whether the statistics collector is up. A tevent for<br/>
   /// a not-queued-for peer is delivered only while the peer is connected: published while it is down, the tevent is dropped,<br/>
   /// and a delivery failure drops the peer's queued stream whole — the subscriber resumes from tevents published after its<br/>
   /// return. Every peer not declared here gets queue-while-down.</summary>
   public TConcreteBuilder DoNotQueueTeventsFor(params EndpointId[] peers)
   {
      AssertStillComposing();
      State.Assert(!peers.Contains(Configuration.Id), () => "An endpoint cannot decline queueing for itself: a peer is another endpoint, and the endpoint's own roster serves its tessages in-boundary.");
      State.Assert(!peers.Intersect(_requiredPeers).Any(), () => "A peer cannot be both required and not-queued-for: requiring a peer means holding everything for it until it is met, declining to queue means keeping nothing for it while it is away.");
      _peersNotQueuedFor.AddRange(peers);
      return Self;
   }

   ///<summary>Declares the endpoint's handler-availability patience: how long a send whose type has no live, unambiguous<br/>
   /// route right now waits for one to appear — a first contact, a known peer's return, an ambiguity resolving — before<br/>
   /// failing loud. Declared at most once; defaults to a flat 30 seconds<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
   public TConcreteBuilder HandlerAvailabilityPatience(TimeSpan patience)
   {
      AssertStillComposing();
      State.Assert(!_handlerAvailabilityPatienceDeclared, () => "The endpoint already declared its handler-availability patience — an endpoint has exactly one.");
      _handlerAvailabilityPatienceDeclared = true;
      _handlerAvailabilityPatience = new HandlerAvailabilityPatience(patience);
      return Self;
   }

   ///<summary>Hands the endpoint the tessages-in-flight tracker its testing composition awaits quiescence through — the<br/>
   /// testing device behind "a test cannot pass with work silently in flight". Production compositions declare none and get<br/>
   /// the do-nothing tracker.</summary>
   internal TConcreteBuilder TrackTessagesInFlightWith(ITessagesInFlightTracker tracker)
   {
      AssertStillComposing();
      _tessagesInFlightTracker = tracker;
      return Self;
   }

   private protected void AssertStillComposing() =>
      State.Assert(!_composed, () => "The endpoint is already composed — the declaration surface exists only inside the composition callback, and the build closes the roster.");

   private protected virtual void AssertTheFoundationIsDeclared()
   {
      State.Assert(_registerTransportProtocol is not null,
                   () => "The endpoint declares no transport protocol. Declare it in the composition — e.g. endpointBuilder.NamedPipeEndpointTransport() or endpointBuilder.AspNetCoreEndpointTransport().");
      State.Assert(_registerSerializer is not null || (Registrar.IsRegistered<ITessagingSerializer>() && Registrar.IsRegistered<ITypermediaSerializer>()),
                   () => "The endpoint declares no serializer. Declare it in the composition — e.g. endpointBuilder.NewtonsoftSerializer() — the endpoint's one serializer parameter. (A testing container cloned from a suite root already carries the suite's serializers; only such a composition declares none.)");
   }

   ///<summary>The registrations both endpoint tiers share: the engine and its doors, identity, discovery serving, the<br/>
   /// transport-speaking substrate (router, peer memory and administration, the best-effort delivery leg, request handling),<br/>
   /// and Typermedia's serving and navigating sides — both tiers serve all four tessage kinds, unconditionally.</summary>
   void RegisterTheSharedEndpointCore()
   {
      Registrar.Register(Singleton.For<EndpointId>().Instance(Configuration.Id),
                         Singleton.For<EndpointConfiguration>().Instance(Configuration),
                         Singleton.For<HandlerAvailabilityPatience>().Instance(_handlerAvailabilityPatience),
                         Singleton.For<ITessagesInFlightTracker>().Instance(_tessagesInFlightTracker ?? new NullOpTessagesInFlightTracker()));

      EndpointDiscoveryQueryExecutor.RegisterWith(Registrar);

      _registerTransportProtocol!(Registrar);
      _registerSerializer?.Invoke(Registrar);

      //The engine and its doors — the same core a plain container's LocalTessagingEngine composes.
      Registrar.RegisterLocalTessagingEngineCore(_engine.HandlerRegistrations)
               .UnitOfWorkTeventPublisher()
               .IndependentTeventPublisher()
               .LocalTypermediaNavigatorSession()
               .IndependentLocalTypermediaNavigator();

      //The transport-speaking substrate: one router, peer memory and its administration, the waiting sends' availability door,
      //the best-effort tevent delivery leg (the RequirePeers/DoNotQueueTeventsFor declarations are captured by the lists),
      //and the tier's request handling.
      Registrar.PeerRegistry()
               .PeerAdministration()
               .TessagingTransport()
               .HandlerAvailability()
               .BestEffortTeventDelivery(_requiredPeers, _peersNotQueuedFor)
               .TessagingTransportMessagePoster()
               .BestEffortTessagingRequestHandlers();

      //Typermedia's serving and navigating sides ride the same substrate: typermedia tessages route through the endpoint's
      //one router, and the one advertisement carries the typermedia types beside the TessageBus ones.
      TypermediaHandlerExecutor.RegisterWith(Registrar);
      Registrar.TypermediaTransportServer()
               .TypermediaTransport()
               .TypermediaRouting()
               .RemoteTypermediaNavigator();
   }

   private protected TBuiltEndpoint BuildEndpoint<TBuiltEndpoint>(Func<IDependencyInjectionContainer, TBuiltEndpoint> createEndpoint) where TBuiltEndpoint : Endpoint
   {
      AssertTheFoundationIsDeclared();
      //The tier registers first: the shared substrate adapts to what the tier declares - the peer registry's durability
      //follows the tier's declared persistence, so the sql layers must already be registered when it registers.
      RegisterTheTierMachinery();
      RegisterTheSharedEndpointCore();
      _composed = true;

      var container = _containerBuilder.Build();
      //Compose the type map here, at composition, so a missing or contradicting type-mapping requirement fails while the
      //composition is still on the stack rather than when the endpoint first serializes something.
      container.RootResolver.Resolve<ITypeMap>();
      //The one discovery question every endpoint serves: its answer is the endpoint's identity and its one advertisement -
      //the roster's projection, covering every tessage kind.
      new EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(container.RootResolver.Resolve<EndpointDiscoveryQueryExecutor>())
        .ForQuery((EndpointInformationQuery _, TessageHandlerRoster roster, EndpointConfiguration configuration) =>
                     new EndpointInformation([.. roster.AdvertisedRemoteTessageTypeIds()], configuration));
      AssertTheRosterIsSound(container.RootResolver.Resolve<TessageHandlerRoster>());

      return createEndpoint(container);
   }

   ///<summary>The tier's own registrations — the exactly-once endpoint's durable vertical; the best-effort endpoint adds nothing.</summary>
   private protected virtual void RegisterTheTierMachinery() {}

   ///<summary>Roster soundness fails at composition, not when the first peer queries: computing the advertisement asserts that<br/>
   /// every advertised type has a type-id mapping and gets a route on the peers' routers. The best-effort tier additionally<br/>
   /// asserts that no registered handler demands more than the endpoint delivers.</summary>
   private protected virtual void AssertTheRosterIsSound(TessageHandlerRoster roster) => roster.AdvertisedRemoteTessageTypeIds();

   private protected IReadOnlyList<IEndpointAddressAnnouncer> AddressAnnouncers => _addressAnnouncers;
   private protected IEndpointRegistry? EndpointRegistry { get; private set; }
}
