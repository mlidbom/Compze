using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.EndpointCatalog;
using Compze.Tessaging.TessageBus._private.Inbox;
using Compze.Tessaging.TessageBus._private.Outbox;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus._private;

namespace Compze.Tessaging.Endpoints.ExactlyOnce;

///<summary>The <see cref="EndpointBuilder"/> an <see cref="ExactlyOnceEndpoint"/> is built through — everything the base<br/>
/// declares, plus the domain database this endpoint joins (<see cref="ConfigurePersistence"/>): the<br/>
/// durable vertical — inbox, outbox, durable peer memory — lives in it, and its atomicity <em>is</em> its co-location with<br/>
/// the domain data the endpoint's executions touch. Declared through a database package's named extension<br/>
/// (e.g. <c>SqliteDomainDatabase(...)</c>), which registers the engine pairing — the connection pool, the type-id<br/>
/// interner, and Tessaging's sql layers for that engine — so the pairing is routed by the extension's target type.</summary>
public sealed class ExactlyOnceEndpointBuilder : EndpointBuilder
{
   Action<IComponentRegistrar>? _registerDomainDatabase;
   ExactlyOnce.RetryPolicy? _retryPolicy;

   internal ExactlyOnceEndpointBuilder(IContainerBuilder containerBuilder, EndpointConfiguration configuration) : base(containerBuilder, configuration) {}

   ///<summary>Declares how the endpoint retries a tessage whose handling fails, and when it gives up, marks the tessage failed<br/>
   /// and goes on with the tessages behind it: see <see cref="ExactlyOnce.RetryPolicy"/>. Declared at most once; an endpoint<br/>
   /// that declares nothing gives up after 5 minutes of retrying.</summary>
   public ExactlyOnceEndpointBuilder RetryPolicy(RetryPolicy policy)
   {
      AssertStillComposing();
      State.Assert(_retryPolicy is null, () => "The endpoint already declared its retry policy — an endpoint has exactly one.");
      _retryPolicy = policy;
      return this;
   }

   ///<summary>Declares deferred tommand handlers through the <see cref="DeferredTessageBusHandlerRegistrar"/>: handlers that never<br/>
   /// run in their sender's unit of work. The endpoint's inbox handles each tommand sent to one in a unit of work of its own,<br/>
   /// once the sender has committed, whether this endpoint sent it or another did. Only an exactly-once endpoint has them,<br/>
   /// since only it has an inbox.</summary>
   public ExactlyOnceEndpointBuilder RegisterDeferredTessageBusHandlers(Action<DeferredTessageBusHandlerRegistrar> register)
   {
      //review-naming: RegisterDeferredTessageBusHandlers, after Magnus's choice of Deferred (2026-11-27).
      AssertStillComposing();
      var registrar = new DeferredTessageBusHandlerRegistrar(HandlerRegistrations);
      register(registrar);
      registrar.EndCallback();
      return this;
   }

   //todo:review: This just dealing in an IComponentRegistrar feels questionable
   ///<summary>Declares the domain database this endpoint joins — where the domain data its executions touch lives, and where<br/>
   /// its durable vertical lives with it. Declared exactly once, through a database package's named extension<br/>
   /// (e.g. <c>SqliteDomainDatabase(...)</c>).</summary>
   public ExactlyOnceEndpointBuilder ConfigurePersistence(Action<IComponentRegistrar> registerDomainDatabase)
   {
      AssertStillComposing();
      State.Assert(_registerDomainDatabase is null, () => "The endpoint already declared the domain database it joins — an endpoint joins exactly one.");
      _registerDomainDatabase = registerDomainDatabase;
      return this;
   }

   private protected override void AssertTheFoundationIsDeclared()
   {
      base.AssertTheFoundationIsDeclared();
      State.Assert(_registerDomainDatabase is not null,
                   () => "The endpoint declares no domain database. An exactly-once endpoint's durable vertical — inbox, outbox, durable peer memory — lives in the domain database it joins: declare it in the environment's ConfigureDomainDatabase, e.g. endpointBuilder.SqliteDomainDatabase(...). An endpoint that deliberately persists nothing is a best-effort endpoint instead (BestEffortEndpointDeclaration).");
   }

   ///<summary>The durable vertical: the declared domain database, the endpoint's place in it (the table-set and the endpoint<br/>
   /// catalog's process lock), the inbox (receiver dedup, transactional handling and its retries), the outbox (durable rows, recovery backlog,<br/>
   /// per-peer exactly-once in-order delivery streams), the tommand senders, and the exactly-once request handling that<br/>
   /// receives arriving tessages into the inbox.</summary>
   private protected override void RegisterTheTierMachinery()
   {
      //Computed eagerly, so an endpoint name that cannot key storage fails loud at build, not at first resolution.
      Registrar.Register(Singleton.For<EndpointTableSet>().Instance(EndpointTableSet.For(Configuration)),
                         Singleton.For<RetryPolicy>().Instance(_retryPolicy ?? ExactlyOnce.RetryPolicy.Default));
      _registerDomainDatabase!(Registrar);
      Registrar.EndpointProcessLock()
               .Outbox()
               .Inbox()
               .UnitOfWorkTommandSender()
               .IndependentTommandSender()
               .ExactlyOnceTessagingRequestHandlers();
   }

   internal ExactlyOnceEndpoint Build() =>
      BuildEndpoint(container => new ExactlyOnceEndpoint(container, Configuration, AddressAnnouncers, EndpointRegistry));
}
