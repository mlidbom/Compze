using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport;

namespace Compze.Tessaging.Endpoints;

///<summary>The declaration surface an <see cref="ExactlyOnceEndpoint"/> is composed through — everything the base<br/>
/// <see cref="EndpointBuilder"/> declares, plus the endpoint's database (<see cref="Database"/>): the durable vertical —<br/>
/// inbox, outbox, durable peer memory — lives in it, and its atomicity <em>is</em> its co-location with the domain data the<br/>
/// endpoint's executions touch. Declared through a database package's named extension<br/>
/// (e.g. <c>SqliteEndpointDatabase(...)</c>), which registers the engine pairing — the connection pool, the type-id<br/>
/// interner, and Tessaging's sql layers for that engine — so the pairing is routed by the extension's target type.</summary>
public sealed class ExactlyOnceEndpointBuilder : EndpointBuilder
{
   Action<IComponentRegistrar>? _registerDatabase;

   internal ExactlyOnceEndpointBuilder(IContainerBuilder containerBuilder, EndpointConfiguration configuration) : base(containerBuilder, configuration) {}

   ///<summary>Declares the endpoint's database — where the durable vertical lives. Declared exactly once, through a database<br/>
   /// package's named extension (e.g. <c>SqliteEndpointDatabase(...)</c>).</summary>
   public void Database(Action<IComponentRegistrar> registerDatabase)
   {
      AssertStillComposing();
      State.Assert(_registerDatabase is null, () => "The endpoint already declared its database — an endpoint's durable vertical lives in exactly one.");
      _registerDatabase = registerDatabase;
   }

   private protected override void AssertTheFoundationIsDeclared()
   {
      base.AssertTheFoundationIsDeclared();
      State.Assert(_registerDatabase is not null,
                   () => "The endpoint declares no database. An exactly-once endpoint's durable vertical — inbox, outbox, durable peer memory — lives in its database: declare it in the composition, e.g. endpoint.SqliteEndpointDatabase(...). An endpoint that deliberately persists nothing is a best-effort endpoint instead (BestEffortEndpoint.Compose).");
   }

   ///<summary>The durable vertical: the declared database, the inbox (receiver dedup, transactional retry), the outbox<br/>
   /// (durable rows, recovery backlog, per-peer exactly-once in-order delivery streams), the tommand-sending doors, and the<br/>
   /// exactly-once request handling that receives arriving tessages into the inbox.</summary>
   private protected override void RegisterTheTierMachinery()
   {
      _registerDatabase!(Registrar);
      Registrar.Outbox()
               .Inbox()
               .UnitOfWorkTommandSender()
               .IndependentTommandSender()
               .ExactlyOnceTessagingRequestHandlers();
   }

   internal ExactlyOnceEndpoint Build() =>
      BuildEndpoint(container => new ExactlyOnceEndpoint(container, Configuration, AddressAnnouncers, EndpointRegistry));
}
