using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Configuration;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Typermedia;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

class Client : IClient
{
   readonly IServiceLocator _serviceLocator;
   readonly ITypermediaRouter _typermediaRouter;

   internal Client(IServiceLocator serviceLocator)
   {
      _serviceLocator = serviceLocator;
      _typermediaRouter = serviceLocator.Resolve<ITypermediaRouter>();
   }

   bool _started;

   internal async Task StartAsync(EndPointAddress seedAddress)
   {
      _typermediaRouter.Start();
      await _typermediaRouter.DiscoverAndConnectAsync(seedAddress).caf();
      _started = true;
   }

   internal void Stop()
   {
      if(_started)
      {
         _started = false;
         _typermediaRouter.Stop();
      }
   }

   public static async Task<IClient> ConnectTo(EndPointAddress seedAddress, IDependencyInjectionContainer container)
   {
      var register = container.Register();
      register.JSonAppConfigFileConfigurationParameterProvider()
              .TypeMapper()
              .Transport()
              .RemoteHypermediaNavigator();

      var client = new Client(container.ServiceLocator);
      await client.StartAsync(seedAddress).caf();
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
      Stop();
      await _serviceLocator.DisposeAsync().caf();
   }
}
