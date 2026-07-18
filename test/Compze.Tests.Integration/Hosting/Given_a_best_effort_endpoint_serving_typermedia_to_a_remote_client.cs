using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tessaging.Endpoints;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// A best-effort endpoint (<see cref="BestEffortEndpoint.Build"/>) hosts everything that persists nothing: it serves
/// remote tueries and tommands with no outbox, no inbox, and no database anywhere, and an external client application
/// navigates its typermedia at its address. The durable vertical is exactly what such an endpoint cannot carry: an
/// exactly-once endpoint composed without a database declaration fails loud at composition, naming the missing declaration.
/// The host is the production host — nothing is pre-registered, so the composition stands entirely on what it declares.
///</summary>
public class Given_a_best_effort_endpoint_serving_typermedia_to_a_remote_client : UniversalTestBase
{
   readonly IEndpointHost _host;
   readonly BestEffortEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Given_a_best_effort_endpoint_serving_typermedia_to_a_remote_client()
   {
      //The endpoint's whole state: no database stands behind this endpoint, so what it serves lives in process memory.
      var registeredUsers = new List<UserResource>();

      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      _endpoint = _host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "DatabaselessTypermediaEndpoint",
         new EndpointId(Guid.Parse("d2f9c1a4-6e83-4b57-9a02-8c5d41e7f6b0")),
         endpoint =>
         {
            endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings());
            endpoint.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
            endpoint.NewtonsoftSerializer();

            endpoint.RegisterTessageHandlers(handle => handle
                       .ForTuery((GetUserTuery tuery) => registeredUsers.Single(user => user.Name == tuery.Name))
                       .ForTommand((RegisterUserTypermediaTommand tommand) =>
                        {
                           registeredUsers.Add(new UserResource(tommand.Name));
                           return new UserRegisteredConfirmationResource(tommand.Name);
                        }));
         }));
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.Address!, mapper => mapper.RegisterIntegrationTestTypeMappings()).caf();
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

   [PCT] public async Task composing_an_exactly_once_endpoint_without_a_domain_database_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(
                        container, "TessagingWithoutADatabase", new EndpointId(Guid.NewGuid()),
                        endpoint =>
                        {
                           endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings());
                           endpoint.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
                           endpoint.NewtonsoftSerializer();
                        })))
         .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no domain database");
   }

   protected internal class GetUserTuery(string name) : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<UserResource>
   {
      public string Name { get; private set; } = name;
   }

   protected internal class UserResource(string name)
   {
      public string Name { get; private set; } = name;
   }

   protected internal class RegisterUserTypermediaTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<UserRegisteredConfirmationResource>
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
