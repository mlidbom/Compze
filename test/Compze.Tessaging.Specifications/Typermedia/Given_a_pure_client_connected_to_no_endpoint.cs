using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;
using Compze.Hosting.Testing;
using static Compze.Must.MustActions;
using Compze.Tessaging.Typermedia.Client;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1812 // Instantiated by the serializer / never instantiated test message types

namespace Compze.Tessaging.Specifications.Typermedia;

///<summary>A pure client (<see cref="TypermediaClient"/>) that has connected to no endpoint has no routes: navigating fails<br/>
/// loud with the no-handler failure, never silently.</summary>
public class Given_a_pure_client_connected_to_no_endpoint : UniversalTestBase
{
   readonly TypermediaClient _client;

   public Given_a_pure_client_connected_to_no_endpoint() =>
      _client = TypermediaClient.Compose(TestEnv.DIContainer.CreateTestingContainerBuilder(), compose =>
      {
         compose.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransportClient());
         compose.Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());
      });

   protected override async Task DisposeAsyncInternal() => await _client.DisposeAsync();

   [PCT] public void getting_a_tuery_throws_NoHandlerForTypermediaTypeException() =>
      Invoking(() => _client.Navigator.Get(new SomeTuery())).Must().Throw<NoHandlerForTypermediaTypeException>();

   [PCT] public void posting_a_tommand_throws_NoHandlerForTypermediaTypeException() =>
      Invoking(() => _client.Navigator.Post(SomeTommand.Create())).Must().Throw<NoHandlerForTypermediaTypeException>();

   // ReSharper disable once ClassNeverInstantiated.Local
   class SomeTueryResult;
   class SomeTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<SomeTueryResult>;

   class SomeTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
   {
      SomeTommand() {}
      public static SomeTommand Create() => new() { Id = new TessageId() };
   }
}
