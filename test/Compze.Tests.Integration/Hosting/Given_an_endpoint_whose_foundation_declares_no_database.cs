using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Internals.Transport.AspNet;
using Compze.Internals.Transport.NamedPipes;
using Compze.Must;
using Compze.Tessaging.Hosting;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Typermedia;
using Compze.Typermedia.Client;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Hosting.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// An endpoint whose foundation declares its transport but — deliberately — no database hosts everything that
/// persists nothing: distributed Typermedia serves remote tueries and tommands with no outbox, no inbox, and no
/// database anywhere. Delivery-guaranteed Tessaging is exactly what such an endpoint cannot speak: adding it
/// fails loud at setup time, naming the missing persistence declaration. The host is the production host —
/// nothing is pre-registered, so the composition stands entirely on what it declares.
///</summary>
public class Given_an_endpoint_whose_foundation_declares_no_database : UniversalTestBase
{
   readonly IEndpointHost _host;
   readonly IEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Given_an_endpoint_whose_foundation_declares_no_database()
   {
      //The endpoint's whole state: no database stands behind this endpoint, so what it serves lives in process memory.
      var registeredUsers = new List<UserResource>();

      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      _endpoint = _host.RegisterEndpoint(
         "DatabaselessTypermediaEndpoint",
         new EndpointId(Guid.Parse("d2f9c1a4-6e83-4b57-9a02-8c5d41e7f6b0")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();

            ComposeTheFoundationWithoutADatabase(builder)
              .AddDistributedTypermedia(typermedia => typermedia.NewtonsoftSerializer());

            builder.RegisterTypermediaHandlers
                   .ForTuery((GetUserTuery tuery) => registeredUsers.Single(user => user.Name == tuery.Name))
                   .ForTommandWithResult((RegisterUserTypermediaTommand tommand) =>
                    {
                       registeredUsers.Add(new UserResource(tommand.Name));
                       return new UserRegisteredConfirmationResource(tommand.Name);
                    });
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.TypermediaAddress!, mapper => mapper.RegisterIntegrationTestTypeMappings()).caf();
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

   [PCT] public async Task adding_distributed_tessaging_fails_loud_naming_the_missing_persistence_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint("TessagingWithoutADatabase",
                                           new EndpointId(Guid.NewGuid()),
                                           builder =>
                                           {
                                              builder.TypeMapper.RegisterIntegrationTestTypeMappings();
                                              ComposeTheFoundationWithoutADatabase(builder);
                                              builder.AddDistributedTessaging();
                                           }))
         .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no Tessaging persistence");
   }

   ///<summary>The typed two-stage composition on the current test's transport protocol: the foundation declares the endpoint's<br/>
   /// transport and — the point of these specifications — no database, so it is the plain <see cref="EndpointFoundation"/> that<br/>
   /// only the features persisting nothing can build on.</summary>
   static EndpointFoundation ComposeTheFoundationWithoutADatabase(IEndpointBuilder builder) =>
      builder.ComposeEndpoint(it => TestEnv.Transport switch
      {
         Transport.AspNetCore => it.AspNetCoreEndpointTransport(),
         Transport.NamedPipes => it.NamedPipeEndpointTransport(),
         _ => throw new ArgumentOutOfRangeException()
      });

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
