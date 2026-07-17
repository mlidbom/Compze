using System.Collections.Concurrent;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Two endpoints whose foundations declare their transports but — deliberately — no databases converse in
/// best-effort tevents: guarantee-free Tessaging with no outbox, no inbox, and no database anywhere in either
/// endpoint. Everything exactly-once is exactly what such an endpoint cannot speak: registering a handler for
/// a tessage type declaring the exactly-once contract — either subscription kind — fails loud at setup, and
/// publishing an exactly-once tevent fails loud naming the missing delivery leg. The host is the production
/// host — nothing is pre-registered, so the composition stands entirely on what it declares.
///</summary>
public class Given_two_endpoints_composing_distributed_tessaging_on_foundations_declaring_no_database : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);

   readonly IEndpointHost _host;
   readonly IEndpoint _publisherEndpoint;
   readonly IThreadGate _subscriberBestEffortTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "subscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _bestEffortTeventsHandledOnTheSubscriber = new();

   public Given_two_endpoints_composing_distributed_tessaging_on_foundations_declaring_no_database()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      var endpointsOfTheHost = new TessagingAddressesOfTheHostsEndpoints(() => _host.Endpoints);

      _publisherEndpoint = _host.RegisterEndpoint(
         "DistributedTessagingPublisherEndpoint",
         new EndpointId(Guid.Parse("6d0a3a3e-59c8-4b0b-9e51-2f47a68d31c4")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();
            builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                   .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                   .DiscoverEndpointsThrough(endpointsOfTheHost);
         });

      _host.RegisterEndpoint(
         "DistributedTessagingSubscriberEndpoint",
         new EndpointId(Guid.Parse("a4c1f7d2-8b35-49e0-b6a9-5d21c30e79f8")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();
            builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                   .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                   .DiscoverEndpointsThrough(endpointsOfTheHost)
                   .RegisterTessageHandlers(handle => handle.ForTevent((IMyBestEffortTevent tevent) =>
                    {
                       _bestEffortTeventsHandledOnTheSubscriber.Enqueue(tevent);
                       _subscriberBestEffortTeventHandlerGate.AwaitPassThrough();
                    }));
         });
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void a_best_effort_tevent_published_on_one_endpoint_is_handled_on_the_other()
   {
      PublishOnThePublisherEndpointInATransaction(new MyBestEffortTevent { SequenceNumber = 1 });

      _subscriberBestEffortTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);
      _bestEffortTeventsHandledOnTheSubscriber.Single().SequenceNumber.Must().Be(1);
   }

   [PCT] public void publishing_a_tevent_declaring_the_exactly_once_contract_fails_loud_naming_the_missing_delivery_leg() =>
      Invoking(() => PublishOnThePublisherEndpointInATransaction(new TeventDeclaringTheExactlyOnceContract()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("demands the exactly-once delivery leg");

   [PCT] public async Task registering_a_handler_for_a_tevent_declaring_the_exactly_once_contract_fails_at_setup()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint("ExactlyOnceSubscriptionOnADistributedEndpoint",
                                           new EndpointId(Guid.NewGuid()),
                                           builder => builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                                                             .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                                                             .RegisterTessageHandlers(handle => handle.ForTevent((ITeventDeclaringTheExactlyOnceContract _) => Task.CompletedTask))))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task registering_a_transaction_ignoring_handler_for_a_tevent_declaring_the_exactly_once_contract_fails_at_setup()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint("ExactlyOnceObservationOnADistributedEndpoint",
                                           new EndpointId(Guid.NewGuid()),
                                           builder => builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                                                             .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                                                             .ObserveTevents(observe => observe.ForTevent((ITeventDeclaringTheExactlyOnceContract _) => {}))))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task registering_a_handler_for_a_tommand_declaring_the_exactly_once_contract_fails_at_setup()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint("ExactlyOnceTommandHandlerOnADistributedEndpoint",
                                           new EndpointId(Guid.NewGuid()),
                                           builder => builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                                                             .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                                                             .RegisterTessageHandlers(handle => handle.ForTommand((TommandDeclaringTheExactlyOnceContract _) => Task.CompletedTask))))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task adding_distributed_tessaging_without_a_serializer_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint("DistributedTessagingWithoutASerializer",
                                           new EndpointId(Guid.NewGuid()),
                                           builder => builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                                                             .AddDistributedTessaging(_ => {})))
        .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no Tessaging serializer");
   }

   void PublishOnThePublisherEndpointInATransaction(ITevent tevent) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                                                                                                      unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(tevent));

   ///<summary>Knows the Tessaging address of every endpoint in the host, so each endpoint's router connects to all of them — the<br/>
   /// discovery a production suite gets from a shared registry, with nothing persisted anywhere.</summary>
   class TessagingAddressesOfTheHostsEndpoints(Func<IReadOnlyList<IEndpoint>> hostEndpoints) : IEndpointRegistry
   {
      readonly Func<IReadOnlyList<IEndpoint>> _hostEndpoints = hostEndpoints;

      public IEnumerable<EndpointAddress> ServerEndpointAddresses =>
      [
         .. _hostEndpoints().Where(it => it.TessagingAddress is not null)
                            .Select(it => it.TessagingAddress!)
      ];
   }

   protected internal interface ITeventDeclaringTheExactlyOnceContract : IExactlyOnceTevent;

   protected internal class TeventDeclaringTheExactlyOnceContract : ITeventDeclaringTheExactlyOnceContract
   {
      public TessageId Id { get; } = new();
   }

   protected internal class TommandDeclaringTheExactlyOnceContract : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
