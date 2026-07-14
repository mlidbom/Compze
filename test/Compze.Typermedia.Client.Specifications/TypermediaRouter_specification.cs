using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Typermedia.Hosting.Testing;
using Compze.Typermedia.Hosting.Testing.Wiring;
using Compze.Hosting.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1812 // Instantiated by the serializer / never instantiated test message types

namespace Compze.Typermedia.Client.Specifications;

public class Given_a_started_typermedia_router_with_no_connected_endpoints : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly IRemoteTypermediaNavigator _navigator;

   public Given_a_started_typermedia_router_with_no_connected_endpoints()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar
             .CurrentTestsSerializersIfNotClonedContainer()
             .CurrentTestsTypermediaClientTransport()
             .TypermediaClientTypeIdentifierMapper(_ => {})
             .TypermediaRouter()
             .SingletonRemoteTypermediaNavigator();

      _container = builder.Build();
      _container.Resolve<ITypermediaRouter>().Start();
      _navigator = _container.Resolve<IRemoteTypermediaNavigator>();
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   [PCT] public void getting_a_tuery_throws_NoHandlerForTypermediaTypeException() =>
      Invoking(() => _navigator.Get(new SomeTuery())).Must().Throw<NoHandlerForTypermediaTypeException>();

   [PCT] public void posting_a_tommand_throws_NoHandlerForTypermediaTypeException() =>
      Invoking(() => _navigator.Post(SomeTommand.Create())).Must().Throw<NoHandlerForTypermediaTypeException>();

   // ReSharper disable once ClassNeverInstantiated.Local
   class SomeTueryResult;
   class SomeTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<SomeTueryResult>;

   class SomeTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
   {
      SomeTommand() {}
      public static SomeTommand Create() => new() { Id = new TessageId() };
   }
}
