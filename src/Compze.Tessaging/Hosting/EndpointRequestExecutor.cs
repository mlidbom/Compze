using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.DependencyInjection;

namespace Compze.Tessaging.Hosting;

public static class EndpointRequestExecutor
{
   //Manual request implementions passed a bus to do their job.

   //todo: does it really make sense to "inject" server actions in this way? Should we not simply register a handler in the bus and invoke that handler. This seems to be sort of like mocking. Testing in a fantasy world instead of in a realistic context.
   public static void ExecuteServerRequestInTransaction(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteTransactionInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));

   //todo: Why would we run a "Server request" without a transaction??
   public static void ExecuteServerRequest(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));
}