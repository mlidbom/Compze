using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.Messaging.Hypermedia;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Messaging.Buses;

public static class EndpointRequestExecutor
{
   //Manual request implementions passed a bus to do their job.

   //todo: does it really make sense to "inject" server actions in this way? Should we not simply register a handler in the bus and invoke that handler. This seems to be sort of like mocking. Testing in a fantasy world instead of in a realistic context.
   internal static void ExecuteServerRequestInTransaction(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteTransactionInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));

   //todo: Why would we run a "Server request" without a transaction??
   internal static void ExecuteServerRequest(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));


   //Urgent: Most of these ExecuteClientRequests are very suspect. Are they really endpoint actions or something about pure clients? What is a "client endpoint"?
   internal static void ExecuteClientRequest(this IEndpoint @this, Action<IRemoteHypermediaNavigator> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()));
   internal static TResult ExecuteClientRequest<TResult>(this IEndpoint @this, Func<IRemoteHypermediaNavigator, TResult> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

   internal static async Task<TResult> ExecuteClientRequestAsync<TResult>(this IEndpoint @this, Func<IRemoteHypermediaNavigator, Task<TResult>> request) =>
      await @this.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(@this.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()).CaF()).CaF();

   public static async Task ExecuteClientRequestAsync(this IEndpoint endpoint, Func<Task> request) =>await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request().CaF()).CaF();

   static async Task ExecuteClientRequestAsync(this IEndpoint endpoint, Func<IRemoteHypermediaNavigator, Task> request) =>
      await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(endpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()).CaF()).CaF();

   //Leverage the manual implementations above to enable running navigation specifications as requests
   public static TResult ExecuteClientRequest<TResult>(this IEndpoint @this, NavigationSpecification<TResult> navigation) => @this.ExecuteClientRequest(navigation.NavigateOn);
   static void ExecuteClientRequest(this IEndpoint @this, NavigationSpecification navigation) => @this.ExecuteClientRequest(navigation.NavigateOn);
   internal static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, NavigationSpecification<TResult> navigation) => await endpoint.ExecuteClientRequestAsync(navigation.NavigateOnAsync).CaF();
   static async Task ExecuteClientRequestAsync(this IEndpoint endpoint, NavigationSpecification navigation) => await endpoint.ExecuteClientRequestAsync(navigation.NavigateOnAsync).CaF();

   //Leverage allow for turning it around and access the functionality from the navigation specification instead of from the endpoint. Tastes differ as to which is clearer...
   public static TResult ExecuteAsClientRequestOn<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteClientRequest(navigationSpecification);
   public static void ExecuteAsClientRequestOn(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteClientRequest(navigationSpecification);
   public static async Task<TResult> ExecuteAsClientRequestOnAsync<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification).CaF();
   public static async Task ExecuteAsClientRequestOnAsync(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteClientRequestAsync(navigationSpecification).CaF();
}