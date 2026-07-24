using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tessaging.TessageTypes;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// A best-effort endpoint (<see cref="BestEffortEndpointDeclaration{TIdentity}"/>) hosts everything that persists nothing: it
/// serves remote tueries and tommands with no outbox, no inbox, and no database anywhere, and an external client application
/// navigates its typermedia at its address. The durable vertical is exactly what such an endpoint cannot carry: an
/// exactly-once declaration built in an environment binding no domain database fails loud at build, naming the missing
/// declaration. The host is the production host and the environment is the specification's own — nothing is pre-registered:
/// everything the endpoint is comes from its declaration and its environment.
///</summary>
public class Given_a_best_effort_endpoint_serving_typermedia_to_a_remote_client : UniversalTestBase
{
   readonly IEndpointHost _host;
   readonly BestEffortEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Given_a_best_effort_endpoint_serving_typermedia_to_a_remote_client()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), CurrentTestsBestEffortEnvironment.DeclaringNoTopology());
      _endpoint = _host.RegisterEndpoint(new DatabaselessTypermediaEndpointDeclaration());
   }

   class DatabaselessTypermediaEndpointDeclaration : BestEffortEndpointDeclaration<DatabaselessTypermediaEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "DatabaselessTypermediaEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("D2F9C1A4-6E83-4B57-9A02-8C5D41E7F6B0"));

      ///<summary>The endpoint's whole state: no database stands behind this endpoint, so what it serves lives in process memory.</summary>
      readonly List<UserResource> _registeredUsers = [];

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery((GetUserTuery tuery) => _registeredUsers.Single(user => user.Name == tuery.Name));

      protected override void RegisterTypermediaTommandHandlers(ITypermediaTommandHandlerRegistrar handle) => handle
         .ForTommand((RegisterUserTypermediaTommand tommand) =>
          {
             _registeredUsers.Add(new UserResource(tommand.Name));
             return new UserRegisteredConfirmationResource(tommand.Name);
          });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.Address!, registrar => registrar.RequireIntegrationTestTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await _host.DisposeAsync().caf();
   }

   [PCT] public void a_remote_client_registers_a_user_and_reads_it_back()
   {
      var confirmation = _client.Navigator.Post(RegisterUserTypermediaTommand.Create("first-user"));
      confirmation.Name.Must().Be("first-user");

      var user = _client.Navigator.Get(new GetUserTuery("first-user"));
      user.Name.Must().Be("first-user");
   }

   [PCT] public async Task building_an_exactly_once_endpoint_without_a_domain_database_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), CurrentTestsBestEffortEnvironment.DeclaringNoTopology());
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationWithoutADomainDatabase()))
         .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no domain database");
   }

   ///<summary>An exactly-once declaration whose environment (<see cref="CurrentTestsBestEffortEnvironment"/>) binds no domain database — the missing declaration this specification pins.</summary>
   class EndpointDeclarationWithoutADomainDatabase : ExactlyOnceEndpointDeclaration<EndpointDeclarationWithoutADomainDatabase>, IEndpointIdentity
   {
      public static string Name => "TessagingWithoutADatabase";
      public static EndpointId Id { get; } = new(Guid.Parse("E1FFA46C-AF3E-4B5C-A389-C9CB816AE50F"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   protected internal class GetUserTuery(string name) : Remotable.NonTransactional.Tueries.Tuery<UserResource>
   {
      public string Name { get; private set; } = name;
   }

   protected internal class UserResource(string name)
   {
      public string Name { get; private set; } = name;
   }

   protected internal class RegisterUserTypermediaTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<UserRegisteredConfirmationResource>
   {
      RegisterUserTypermediaTommand() {}

      public static RegisterUserTypermediaTommand Create(string name) => new()
                                                                         {
                                                                            Name = name,
                                                                            Id = new TessageId()
                                                                         };

      public string Name { get; private init; } = "";
   }

   protected internal class UserRegisteredConfirmationResource(string name)
   {
      public string Name { get; } = name;
   }
}
