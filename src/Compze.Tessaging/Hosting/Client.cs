using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

class Client(IEndpoint innerEndpoint) : IClient
{
   public void ExecuteRequest(Action<IRemoteTypermediaNavigator> request) =>
      innerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => request(innerEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>()));

   public TResult ExecuteRequest<TResult>(Func<IRemoteTypermediaNavigator, TResult> request) =>
      innerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => request(innerEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>()));

   public async Task<TResult> ExecuteRequestAsync<TResult>(Func<IRemoteTypermediaNavigator, Task<TResult>> request) =>
      await innerEndpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(innerEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>()).caf()).caf();

   public async Task ExecuteRequestAsync(Func<IRemoteTypermediaNavigator, Task> request) =>
      await innerEndpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(innerEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>()).caf()).caf();

   public async ValueTask DisposeAsync() => await innerEndpoint.DisposeAsync().caf();
}
