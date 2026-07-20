using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tessaging;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.TessageTypes;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Two best-effort endpoints (<see cref="BestEffortEndpoint.Build"/>) converse in typermedia — request/response neither
/// queues nor persists, so the tier serves it identically to the exactly-once tier. The asking endpoint navigates the
/// answering endpoint's tueries and tommands through its own <see cref="IRemoteTypermediaNavigator"/>, routed by its router's
/// live reconciliation against the registry the endpoint declared it discovers through — never by a configured address. The
/// answering endpoint declares no registry: it only serves, and navigating from it fails loud naming the missing
/// declaration. The host is the production host — nothing is pre-registered, so the composition stands entirely on what it
/// declares.
///</summary>
public class Given_two_best_effort_endpoints_conversing_in_typermedia : UniversalTestBase
{
   readonly IEndpointHost _host;
   readonly BestEffortEndpoint _askingEndpoint;
   readonly BestEffortEndpoint _answeringEndpoint;

   public Given_two_best_effort_endpoints_conversing_in_typermedia()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      var endpointsOfTheHost = new AddressesOfTheHostsEndpoints(() => _host.Endpoints);

      _askingEndpoint = _host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "TypermediaAskingEndpoint",
         new EndpointId(Guid.Parse("3f7b9c25-81d4-4a6e-b0f2-c58a17d93e46")),
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
            .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
            .NewtonsoftSerializer()
            .DiscoverEndpointsThrough(endpointsOfTheHost)));

      _answeringEndpoint = _host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "TypermediaAnsweringEndpoint",
         new EndpointId(Guid.Parse("b93d40e7-2c58-4f1b-a6d9-04e8c6a25f17")),
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
            .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
            .NewtonsoftSerializer()
            .RegisterTessageHandlers(handle => handle
                .ForTuery((GetTheAnswerTuery _) => new AnswerResource(answeredBy: "TypermediaAnsweringEndpoint"))
                .ForTommand((RegisterGreetingTypermediaTommand tommand) => new GreetingRegisteredConfirmationResource(tommand.Greeting)))));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void a_tuery_executed_on_one_endpoint_is_answered_by_the_endpoint_handling_its_type() =>
      NavigateFromTheAskingEndpoint(navigator => navigator.Get(new GetTheAnswerTuery()))
        .AnsweredBy.Must().Be("TypermediaAnsweringEndpoint");

   [PCT] public void a_tommand_posted_on_one_endpoint_is_handled_by_the_endpoint_handling_its_type_and_its_result_comes_back() =>
      NavigateFromTheAskingEndpoint(navigator => navigator.Post(RegisterGreetingTypermediaTommand.Create("hello from the asking endpoint")))
        .Greeting.Must().Be("hello from the asking endpoint");

   [PCT] public void navigating_from_the_endpoint_that_declared_no_discovery_registry_fails_loud_naming_the_missing_declaration() =>
      Invoking(() => _answeringEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(
                  scope => scope.Resolve<IRemoteTypermediaNavigator>().Get(new GetTheAnswerTuery())))
        .Must().Throw<Exception>().Which.Message.Must().Contain("DiscoverEndpointsThrough/ParticipateIn");

   TResult NavigateFromTheAskingEndpoint<TResult>(Func<IRemoteTypermediaNavigator, TResult> navigate) =>
      _askingEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(scope => navigate(scope.Resolve<IRemoteTypermediaNavigator>()));

   ///<summary>Knows the address of every endpoint in the host, so each endpoint's router connects to all of them — the<br/>
   /// discovery a production suite gets from a shared registry, with nothing persisted anywhere.</summary>
   class AddressesOfTheHostsEndpoints(Func<IReadOnlyList<IEndpoint>> hostEndpoints) : IEndpointRegistry
   {
      readonly Func<IReadOnlyList<IEndpoint>> _hostEndpoints = hostEndpoints;

      public IEnumerable<EndpointAddress> ServerEndpointAddresses => [.._hostEndpoints().OfType<Endpoint>()
                                                                                        .Where(it => it.Address is not null)
                                                                                        .Select(it => it.Address!)];
   }

   protected internal class GetTheAnswerTuery : Remotable.NonTransactional.Tueries.Tuery<AnswerResource>;

   protected internal class AnswerResource(string answeredBy)
   {
      public string AnsweredBy { get; private set; } = answeredBy;
   }

   protected internal class RegisterGreetingTypermediaTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<GreetingRegisteredConfirmationResource>
   {
      RegisterGreetingTypermediaTommand() {}

      public static RegisterGreetingTypermediaTommand Create(string greeting) => new()
                                                                                 {
                                                                                    Greeting = greeting,
                                                                                    Id = new TessageId()
                                                                                 };

      public string Greeting { get; private init; } = "";
   }

   protected internal class GreetingRegisteredConfirmationResource(string greeting)
   {
      public string Greeting { get; } = greeting;
   }
}
