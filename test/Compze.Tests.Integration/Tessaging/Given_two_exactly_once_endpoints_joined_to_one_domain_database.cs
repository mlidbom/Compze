using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Underscore;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>The domain database is the domain's, never an endpoint's: any number of endpoints join one, each storing in its<br/>
/// own prefixed table-set (<see cref="EndpointTableSet"/>) and sharing the domain-level tables — the endpoint catalog, the<br/>
/// type-id interner — and they converse exactly-once through their prefixed outboxes and inboxes side by side in the one<br/>
/// database. The catalog is also where the domain database's endpoint rules are enforced: a name only ever belongs to one<br/>
/// endpoint, and an endpoint id never silently re-keys itself under a new name.</summary>
public class Given_two_exactly_once_endpoints_joined_to_one_domain_database : UniversalTestBase
{
   static readonly EndpointId FirstNeighborEndpointId = new(Guid.Parse("6E19D4A7-3B85-4C02-9F76-D2A81B50E3C4"));
   static readonly EndpointId SecondNeighborEndpointId = new(Guid.Parse("B75F20C8-914E-4D6B-A3E9-58C1D7F4062A"));
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

      _firstNeighbor = _host.RegisterExactlyOnceEndpointInDomainDatabase(
         "FirstNeighbor",
         FirstNeighborEndpointId,
         DomainDatabaseName,
         endpoint =>
         {
            endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings());
            endpoint.RegisterTessageHandlers(handle => handle
                       .ForTommand((MyReplyTommandHandledByTheFirstNeighbor _) =>
                        {
                           _firstNeighborReplyHandlerGate.AwaitPassThrough();
                           return Task.CompletedTask;
                        }));
         });

      _secondNeighbor = _host.RegisterExactlyOnceEndpointInDomainDatabase(
         "SecondNeighbor",
         SecondNeighborEndpointId,
         DomainDatabaseName,
         endpoint =>
         {
            endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings());
            endpoint.RegisterTessageHandlers(handle => handle
                       .ForTommand((MyTommandHandledByTheSecondNeighbor _) =>
                        {
                           _secondNeighborHandlerGate.AwaitPassThrough();
                           return Task.CompletedTask;
                        }));
         });
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

   [PCT] public async Task the_endpoint_catalog_lists_both_neighbors()
   {
      var entries = await _firstNeighbor.ServiceLocator.Resolve<ITessagingSqlLayer.IEndpointCatalogSqlLayer>().GetEntriesAsync();

      entries.Count.Must().Be(2);
      var endpointNames = entries.Select(entry => entry.EndpointName).ToList();
      endpointNames.Must().Contain("FirstNeighbor");
      endpointNames.Must().Contain("SecondNeighbor");
   }

   [PCT] public async Task a_third_endpoint_claiming_an_occupied_name_with_a_different_id_fails_loud_naming_the_collision()
   {
      var otherHost = TestingEndpointHost.Create(_rootContainer);
      otherHost.RegisterExactlyOnceEndpointInDomainDatabase(
         "FirstNeighbor",
         new EndpointId(Guid.NewGuid()),
         DomainDatabaseName,
         endpoint => endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings()));

      var startFailure = (await InvokingAsync(async () => await otherHost.StartAsync()).Must().ThrowAsync<AggregateException>()).Which;
      startFailure.Flatten().InnerExceptions.Single().Message.Must()
                  .Contain("FirstNeighbor")
                  .Contain("taken in this domain database's endpoint catalog");

      await otherHost.DisposeAsync();
   }

   [PCT] public async Task an_endpoint_reclaiming_its_id_under_a_new_name_fails_loud_naming_the_remembered_name()
   {
      var otherHost = TestingEndpointHost.Create(_rootContainer);
      otherHost.RegisterExactlyOnceEndpointInDomainDatabase(
         "RenamedNeighbor",
         FirstNeighborEndpointId,
         DomainDatabaseName,
         endpoint => endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings()));

      var startFailure = (await InvokingAsync(async () => await otherHost.StartAsync()).Must().ThrowAsync<AggregateException>()).Which;
      startFailure.Flatten().InnerExceptions.Single().Message.Must()
                  .Contain("registered in this domain database's endpoint catalog under the name 'FirstNeighbor'")
                  .Contain("RenamedNeighbor");

      await otherHost.DisposeAsync();
   }
}

public class MyTommandHandledByTheSecondNeighbor : TessageTypes.Remotable.ExactlyOnce.Tommand;

public class MyReplyTommandHandledByTheFirstNeighbor : TessageTypes.Remotable.ExactlyOnce.Tommand;
