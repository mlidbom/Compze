using System.Net;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.AcrossMachines;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.Hosting;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.AcrossMachines;

///<summary>Two best-effort endpoints, each in a production host of its own with a <see cref="NetworkEndpointRegistry"/> of<br/>
/// its own, as two processes on two machines would have them. Both registries are opened on the same name on one<br/>
/// <see cref="EndpointRegistryServer"/>, and that is all the two hosts share: each endpoint announces where it listens<br/>
/// through its host's registry, and finds the other through it. The endpoints converse over the current test's transport;<br/>
/// the registry is reached over TCP whatever that transport is.</summary>
public class Given_two_best_effort_endpoints_in_hosts_of_their_own_each_participating_in_the_same_registry_on_a_registry_server : UniversalTestBase
{
   const string RegistryName = "the registry both hosts open";

   readonly EndpointRegistryServer _server = EndpointRegistryServer.StartListeningAt(new IPEndPoint(IPAddress.Loopback, port: 0));
   readonly NetworkEndpointRegistry _registryOfTheAskingHost;
   readonly NetworkEndpointRegistry _registryOfTheAnsweringHost;
   readonly IEndpointHost _askingHost;
   readonly IEndpointHost _answeringHost;
   readonly BestEffortEndpoint _askingEndpoint;

   public Given_two_best_effort_endpoints_in_hosts_of_their_own_each_participating_in_the_same_registry_on_a_registry_server()
   {
      _registryOfTheAskingHost = NetworkEndpointRegistry.OpenOnServerAt(RegistryName, _server.ListensAt);
      _registryOfTheAnsweringHost = NetworkEndpointRegistry.OpenOnServerAt(RegistryName, _server.ListensAt);

      _askingHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), CurrentTestsBestEffortEnvironment.ParticipatingIn(_registryOfTheAskingHost));
      _askingEndpoint = _askingHost.RegisterEndpoint(new AskingEndpointDeclaration());

      _answeringHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), CurrentTestsBestEffortEnvironment.ParticipatingIn(_registryOfTheAnsweringHost));
      _answeringHost.RegisterEndpoint(new AnsweringEndpointDeclaration());
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _askingHost.StartAsync();
      await _answeringHost.StartAsync();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _askingHost.DisposeAsync();
      await _answeringHost.DisposeAsync();
      await _registryOfTheAskingHost.DisposeAsync();
      await _registryOfTheAnsweringHost.DisposeAsync();
      await _server.DisposeAsync();
   }

   [PCT] public void a_tuery_executed_on_one_endpoint_is_answered_by_the_endpoint_in_the_other_host() =>
      _askingEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(scope => scope.Resolve<IRemoteTypermediaNavigator>().Get(new WhoAnswersTuery()))
                     .AnsweredBy.Must().Be(AnsweringEndpointDeclaration.Name);

   class AskingEndpointDeclaration : BestEffortEndpointDeclaration<AskingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "EndpointAskingThroughARegistryServer";
      public static EndpointId Id { get; } = new(Guid.Parse("6B2E8F14-9C37-4D5A-A1E0-73F4C8D25B96"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   class AnsweringEndpointDeclaration : BestEffortEndpointDeclaration<AnsweringEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "EndpointAnsweringThroughARegistryServer";
      public static EndpointId Id { get; } = new(Guid.Parse("E47A1D63-5F08-4B92-8C3E-0A9D6B17F452"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery((WhoAnswersTuery _) => new Answer(answeredBy: Name));
   }

   protected internal class WhoAnswersTuery : Remotable.NonTransactional.Tueries.Tuery<Answer>;

   protected internal class Answer(string answeredBy)
   {
      public string AnsweredBy { get; private set; } = answeredBy;
   }
}
