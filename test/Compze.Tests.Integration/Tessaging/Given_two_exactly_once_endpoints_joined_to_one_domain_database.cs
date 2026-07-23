using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>The domain database is the domain's, never an endpoint's: any number of endpoints join one, each storing in its<br/>
/// own prefixed table-set (<c>EndpointTableSet</c>) and sharing the domain-level tables — the endpoint catalog, the<br/>
/// type-id interner — and they converse exactly-once through their prefixed outboxes and inboxes side by side in the one<br/>
/// database. The catalog is also where the domain database's endpoint rules are enforced: a name only ever belongs to one<br/>
/// endpoint, and an endpoint id never silently re-keys itself under a new name.</summary>
public class Given_two_exactly_once_endpoints_joined_to_one_domain_database : UniversalTestBase
{
   const string DomainDatabaseName = "NeighborsDomainDatabase";

   readonly IDependencyInjectionContainer _rootContainer;
   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _firstNeighbor;
   readonly ExactlyOnceEndpoint _secondNeighbor;
   readonly IThreadGate _secondNeighborHandlerGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "SecondNeighborHandler");
   readonly IThreadGate _firstNeighborReplyHandlerGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "FirstNeighborReplyHandler");

   public Given_two_exactly_once_endpoints_joined_to_one_domain_database()
   {
      //The database pool lives in the root container, so every host sharing it resolves DomainDatabaseName to the one
      //domain database - including the extra hosts the catalog-rule specifications below start endpoints from.
      _rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                              ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                              .Build();
      _host = TestingEndpointHost.Create(_rootContainer);

      _firstNeighbor = _host.RegisterEndpoint(new FirstNeighborEndpointDeclaration(_firstNeighborReplyHandlerGate), new EnvironmentJoiningTheNeighborsDomainDatabase(_host.Environment));
      _secondNeighbor = _host.RegisterEndpoint(new SecondNeighborEndpointDeclaration(_secondNeighborHandlerGate), new EnvironmentJoiningTheNeighborsDomainDatabase(_host.Environment));
   }

   class FirstNeighborEndpointDeclaration : ExactlyOnceEndpointDeclaration<FirstNeighborEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "FirstNeighbor";
      public static EndpointId Id { get; } = new(Guid.Parse("6E19D4A7-3B85-4C02-9F76-D2A81B50E3C4"));

      readonly IThreadGate _replyHandlerGate;
      internal FirstNeighborEndpointDeclaration(IThreadGate replyHandlerGate) => _replyHandlerGate = replyHandlerGate;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyReplyTommandHandledByTheFirstNeighbor _) =>
          {
             _replyHandlerGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }

   class SecondNeighborEndpointDeclaration : ExactlyOnceEndpointDeclaration<SecondNeighborEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "SecondNeighbor";
      public static EndpointId Id { get; } = new(Guid.Parse("B75F20C8-914E-4D6B-A3E9-58C1D7F4062A"));

      readonly IThreadGate _handlerGate;
      internal SecondNeighborEndpointDeclaration(IThreadGate handlerGate) => _handlerGate = handlerGate;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyTommandHandledByTheSecondNeighbor _) =>
          {
             _handlerGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }

   class EndpointDeclarationClaimingTheFirstNeighborsName : ExactlyOnceEndpointDeclaration<EndpointDeclarationClaimingTheFirstNeighborsName>, IEndpointIdentity
   {
      public static string Name => FirstNeighborEndpointDeclaration.Name;
      public static EndpointId Id { get; } = new(Guid.Parse("B088560F-C004-4640-8E60-A58503FD949D"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   class EndpointDeclarationReclaimingTheFirstNeighborsIdUnderANewName : ExactlyOnceEndpointDeclaration<EndpointDeclarationReclaimingTheFirstNeighborsIdUnderANewName>, IEndpointIdentity
   {
      public static string Name => "RenamedNeighbor";
      public static EndpointId Id => FirstNeighborEndpointDeclaration.Id;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   ///<summary>The wrapped <see cref="IEndpointEnvironment"/> except that the endpoint joins the neighbors' shared domain<br/>
   /// database (<see cref="DomainDatabaseName"/>) instead of one of its own — the composition for several endpoints storing<br/>
   /// side by side in one domain database.</summary>
   class EnvironmentJoiningTheNeighborsDomainDatabase : IEndpointEnvironment
   {
      readonly IEndpointEnvironment _environment;
      internal EnvironmentJoiningTheNeighborsDomainDatabase(IEndpointEnvironment environment) => _environment = environment;

      public void DeclareOn(EndpointBuilder endpointBuilder) =>
         _environment.DeclareOn(endpointBuilder);

      public void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder) =>
         endpointBuilder.ConfigurePersistence(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: DomainDatabaseName));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal()
   {
      await _host.DisposeAsync();
      await _rootContainer.DisposeAsync();
   }

   [PCT] public async Task a_tommand_sent_to_the_second_neighbor_is_handled_there_and_its_reply_tommand_comes_back()
   {
      await _firstNeighbor.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyTommandHandledByTheSecondNeighbor());
      _secondNeighborHandlerGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));

      await _secondNeighbor.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyReplyTommandHandledByTheFirstNeighbor());
      _firstNeighborReplyHandlerGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }


   [PCT] public async Task a_third_endpoint_claiming_an_occupied_name_with_a_different_id_fails_loud_naming_the_collision()
   {
      var otherHost = TestingEndpointHost.Create(_rootContainer);
      otherHost.RegisterEndpoint(new EndpointDeclarationClaimingTheFirstNeighborsName(), new EnvironmentJoiningTheNeighborsDomainDatabase(otherHost.Environment));

      var startFailure = (await InvokingAsync(async () => await otherHost.StartAsync()).Must().ThrowAsync<AggregateException>()).Which;
      startFailure.Flatten().InnerExceptions.Single().Message.Must()
                  .Contain(FirstNeighborEndpointDeclaration.Name)
                  .Contain("taken in this domain database's endpoint catalog");

      await otherHost.DisposeAsync();
   }

   [PCT] public async Task an_endpoint_reclaiming_its_id_under_a_new_name_fails_loud_naming_the_remembered_name()
   {
      var otherHost = TestingEndpointHost.Create(_rootContainer);
      otherHost.RegisterEndpoint(new EndpointDeclarationReclaimingTheFirstNeighborsIdUnderANewName(), new EnvironmentJoiningTheNeighborsDomainDatabase(otherHost.Environment));

      var startFailure = (await InvokingAsync(async () => await otherHost.StartAsync()).Must().ThrowAsync<AggregateException>()).Which;
      startFailure.Flatten().InnerExceptions.Single().Message.Must()
                  .Contain($"registered in this domain database's endpoint catalog under the name '{FirstNeighborEndpointDeclaration.Name}'")
                  .Contain(EndpointDeclarationReclaimingTheFirstNeighborsIdUnderANewName.Name);

      await otherHost.DisposeAsync();
   }
}

public class MyTommandHandledByTheSecondNeighbor : Remotable.ExactlyOnce.Tommand;

public class MyReplyTommandHandledByTheFirstNeighbor : Remotable.ExactlyOnce.Tommand;
