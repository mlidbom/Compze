using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Configuration;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Typermedia.Client;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Typermedia;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Tessaging;

public class TestClient : IAsyncDisposable
{
   readonly IDependencyInjectionContainer _container;
   readonly ITypermediaRouter _typermediaRouter;

   public IRemoteTypermediaNavigator Navigator { get; }

   TestClient(IDependencyInjectionContainer container)
   {
      _container = container;
      _typermediaRouter = container.Resolve<ITypermediaRouter>();
      Navigator = container.Resolve<IRemoteTypermediaNavigator>();
   }

   public static async Task<TestClient> ConnectTo(EndpointAddress typermediaAddress, Action<ITypeMapper> registerDomainTypeMappings)
   {
#pragma warning disable CA2000 // We are passing this disposable into a constructor of an object we don't own
        var builder = TestEnv.DIContainer.CreateWithContainerRegistrations();
#pragma warning restore CA2000
        builder.Registrar
               .CurrentTestsSerializersIfNotClonedContainer()
               .CurrentTestsClientTransport()
               .JSonAppConfigFileConfigurationParameterProvider()
               .TypeIdentifierMapper(registerDomainTypeMappings)
               .TypermediaRouter()
               .SingletonRemoteTypermediaNavigator();

      var client = new TestClient(builder.Build());
      client._typermediaRouter.Start();
      await client._typermediaRouter.ConnectAsync(typermediaAddress).caf();
      return client;
   }

   public async ValueTask DisposeAsync()
   {
      _typermediaRouter.Stop();
      await _container.DisposeAsync().caf();
   }
}
