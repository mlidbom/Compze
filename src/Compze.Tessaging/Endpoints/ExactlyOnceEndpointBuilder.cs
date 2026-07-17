using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.EndpointCatalog;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Endpoints;

///<summary>The declaration surface an <see cref="ExactlyOnceEndpoint"/> is composed through — everything the base<br/>
/// <see cref="EndpointBuilder"/> declares, plus the domain database this endpoint joins (<see cref="DomainDatabase"/>): the<br/>
/// durable vertical — inbox, outbox, durable peer memory — lives in it, and its atomicity <em>is</em> its co-location with<br/>
/// the domain data the endpoint's executions touch. Declared through a database package's named extension<br/>
/// (e.g. <c>SqliteDomainDatabase(...)</c>), which registers the engine pairing — the connection pool, the type-id<br/>
/// interner, and Tessaging's sql layers for that engine — so the pairing is routed by the extension's target type.</summary>
public sealed class ExactlyOnceEndpointBuilder : EndpointBuilder
{
   Action<IComponentRegistrar>? _registerDomainDatabase;
   ProcessLeaseDuration _processLeaseDuration = Implementation.EndpointCatalog.ProcessLeaseDuration.Default;
   bool _processLeaseDurationDeclared;

   internal ExactlyOnceEndpointBuilder(IContainerBuilder containerBuilder, EndpointConfiguration configuration) : base(containerBuilder, configuration) {}

   ///<summary>Declares how long the endpoint's process lease stays valid without a heartbeat — the knob the endpoint<br/>
   /// catalog's one-process-per-endpoint rule turns on: a holder heartbeating within the duration counts as alive, a starting<br/>
   /// process waits out at most one duration for a dead predecessor's lease to go stale before taking over, and only a holder<br/>
   /// proven alive by its heartbeats makes the start fail loud<br/>
   /// (<see cref="EndpointAlreadyRunningInAnotherProcessException"/>). Declared at most once; defaults to 15 seconds.</summary>
   public void ProcessLeaseDuration(TimeSpan duration)
   {
      AssertStillComposing();
      State.Assert(!_processLeaseDurationDeclared, () => "The endpoint already declared its process-lease duration — an endpoint has exactly one.");
      _processLeaseDurationDeclared = true;
      _processLeaseDuration = new ProcessLeaseDuration(duration);
   }

   ///<summary>Declares the domain database this endpoint joins — where the domain data its executions touch lives, and where<br/>
   /// its durable vertical lives with it. Declared exactly once, through a database package's named extension<br/>
   /// (e.g. <c>SqliteDomainDatabase(...)</c>).</summary>
   public void DomainDatabase(Action<IComponentRegistrar> registerDomainDatabase)
   {
      AssertStillComposing();
      State.Assert(_registerDomainDatabase is null, () => "The endpoint already declared the domain database it joins — an endpoint joins exactly one.");
      _registerDomainDatabase = registerDomainDatabase;
   }

   private protected override void AssertTheFoundationIsDeclared()
   {
      base.AssertTheFoundationIsDeclared();
      State.Assert(_registerDomainDatabase is not null,
                   () => "The endpoint declares no domain database. An exactly-once endpoint's durable vertical — inbox, outbox, durable peer memory — lives in the domain database it joins: declare it in the composition, e.g. endpoint.SqliteDomainDatabase(...). An endpoint that deliberately persists nothing is a best-effort endpoint instead (BestEffortEndpoint.Compose).");
   }

   ///<summary>The durable vertical: the declared domain database, the endpoint's place in it (the table-set and the endpoint<br/>
   /// catalog's process lease), the inbox (receiver dedup, transactional retry), the outbox (durable rows, recovery backlog,<br/>
   /// per-peer exactly-once in-order delivery streams), the tommand-sending doors, and the exactly-once request handling that<br/>
   /// receives arriving tessages into the inbox.</summary>
   private protected override void RegisterTheTierMachinery()
   {
      //Computed eagerly, so an endpoint name that cannot key storage fails loud at composition, not at first resolution.
      Registrar.Register(Singleton.For<EndpointTableSet>().Instance(EndpointTableSet.For(Configuration)),
                         Singleton.For<ProcessLeaseDuration>().Instance(_processLeaseDuration));
      _registerDomainDatabase!(Registrar);
      Registrar.EndpointProcessLease()
               .Outbox()
               .Inbox()
               .UnitOfWorkTommandSender()
               .IndependentTommandSender()
               .ExactlyOnceTessagingRequestHandlers();
   }

   internal ExactlyOnceEndpoint Build() =>
      BuildEndpoint(container => new ExactlyOnceEndpoint(container, Configuration, AddressAnnouncers, EndpointRegistry));
}
