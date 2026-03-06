using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Configuration;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Typermedia;
using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging;

public class TestClient : IClient
{
   readonly IServiceLocator _serviceLocator;
   readonly ITypermediaRouter _typermediaRouter;

   TestClient(IServiceLocator serviceLocator)
   {
      _serviceLocator = serviceLocator;
      _typermediaRouter = serviceLocator.Resolve<ITypermediaRouter>();
   }

   public static async Task<IClient> ConnectTo(EndPointAddress seedAddress)
   {
#pragma warning disable CA2000 // We are passing this disposable into a constructor of an object we don't own
        var container = TestEnv.DIContainer.CreateWithServiceLocator();
#pragma warning restore CA2000
        container.Register()
               .CurrentTestsSerializersIfNotClonedContainer()
               .CurrentTestsClientTransport()
               .JSonAppConfigFileConfigurationParameterProvider()
               .TypeMapper()
               .TypermediaTransport()
               .RemoteHypermediaNavigator();

      var client = new TestClient(container.ServiceLocator);
      client._typermediaRouter.Start();
      await client._typermediaRouter.DiscoverAndConnectAsync(seedAddress).caf();
      return client;
   }

   public void ExecuteRequest(Action<IRemoteTypermediaNavigator> request) =>
      _serviceLocator.ExecuteInIsolatedScope(() => request(_serviceLocator.Resolve<IRemoteTypermediaNavigator>()));

   public TResult ExecuteRequest<TResult>(Func<IRemoteTypermediaNavigator, TResult> request) =>
      _serviceLocator.ExecuteInIsolatedScope(() => request(_serviceLocator.Resolve<IRemoteTypermediaNavigator>()));

   public async Task<TResult> ExecuteRequestAsync<TResult>(Func<IRemoteTypermediaNavigator, Task<TResult>> request) =>
      await _serviceLocator.ExecuteInIsolatedScopeAsync(async () => await request(_serviceLocator.Resolve<IRemoteTypermediaNavigator>()).caf()).caf();

   public async Task ExecuteRequestAsync(Func<IRemoteTypermediaNavigator, Task> request) =>
      await _serviceLocator.ExecuteInIsolatedScopeAsync(async () => await request(_serviceLocator.Resolve<IRemoteTypermediaNavigator>()).caf()).caf();

   public async ValueTask DisposeAsync()
   {
      _typermediaRouter.Stop();
      await _serviceLocator.DisposeAsync().caf();
   }
}
