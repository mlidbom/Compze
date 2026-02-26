using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

class Client : IClient
{
   readonly IServiceLocator _serviceLocator;
   readonly ITypermediaRoutingClient _typermediaRoutingClient;
   readonly IEndpointRegistry _endpointRegistry;

   public Client(IServiceLocator serviceLocator)
   {
      _serviceLocator = serviceLocator;
      _typermediaRoutingClient = serviceLocator.Resolve<ITypermediaRoutingClient>();
      _endpointRegistry = serviceLocator.Resolve<IEndpointRegistry>();
   }

   bool _started;

   internal async Task StartAsync()
   {
      _typermediaRoutingClient.Start();
      var serverAddresses = _endpointRegistry.ServerEndpointAddresses.ToHashSet();
      await Task.WhenAll(serverAddresses.Select(address => _typermediaRoutingClient.ConnectAsync(address))).caf();
      _started = true;
   }

   internal void Stop()
   {
      if(_started)
      {
         _started = false;
         _typermediaRoutingClient.Stop();
      }
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
