using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting;

public static class EndpointRequestExecutor
{
   //Manual request implementions passed a bus to do their job.

   //todo:review: does it really make sense to "inject" server actions in this way? Should we not simply register a handler in the bus and invoke that handler. This seems to be sort of like mocking. Testing in a fantasy world instead of in a realistic context.
   public static void ExecuteServerRequestInTransaction(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(scopeResolver => request(scopeResolver.Resolve<IServiceBusSession>()));

   //todo:review: Why would we run a "Server request" without a transaction??
   public static void ExecuteServerRequest(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(scopeResolver => request(scopeResolver.Resolve<IServiceBusSession>()));
}
