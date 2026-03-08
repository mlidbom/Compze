using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Configuration;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Typermedia.Client;
using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Tessaging;

public class TestClient : IAsyncDisposable
{
   readonly IServiceLocator _serviceLocator;
   readonly ITypermediaRouter _typermediaRouter;

   public IRemoteTypermediaNavigator Navigator { get; }

   TestClient(IServiceLocator serviceLocator)
   {
      _serviceLocator = serviceLocator;
      _typermediaRouter = serviceLocator.Resolve<ITypermediaRouter>();
      Navigator = serviceLocator.Resolve<IRemoteTypermediaNavigator>();
   }

   public static async Task<TestClient> ConnectTo(EndPointAddress seedAddress)
   {
#pragma warning disable CA2000 // We are passing this disposable into a constructor of an object we don't own
        var container = TestEnv.DIContainer.CreateWithServiceLocator();
#pragma warning restore CA2000
        container.Register()
               .CurrentTestsSerializersIfNotClonedContainer()
               .CurrentTestsClientTransport()
               .JSonAppConfigFileConfigurationParameterProvider()
               .TypeMapper()
               .TypermediaRouter()
               .SingletonRemoteTypermediaNavigator();

      var client = new TestClient(container.ServiceLocator);
      client._typermediaRouter.Start();
      await client._typermediaRouter.ConnectAsync(seedAddress).caf();
      return client;
   }

   public async ValueTask DisposeAsync()
   {
      _typermediaRouter.Stop();
      await _serviceLocator.DisposeAsync().caf();
   }
}
