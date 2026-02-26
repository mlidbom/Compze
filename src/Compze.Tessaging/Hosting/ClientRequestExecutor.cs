using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

public static class ClientRequestExecutor
{
   //NavigationSpecification overloads on IClient
   public static TResult ExecuteRequest<TResult>(this IClient client, NavigationSpecification<TResult> navigation) => client.ExecuteRequest(navigation.NavigateOn);
   public static void ExecuteRequest(this IClient client, NavigationSpecification navigation) => client.ExecuteRequest(navigation.NavigateOn);
   public static async Task<TResult> ExecuteRequestAsync<TResult>(this IClient client, NavigationSpecification<TResult> navigation) => await client.ExecuteRequestAsync(navigation.NavigateOnAsync).caf();
   public static async Task ExecuteRequestAsync(this IClient client, NavigationSpecification navigation) => await client.ExecuteRequestAsync(navigation.NavigateOnAsync).caf();

   //Reverse: navigate from the specification to the client
   public static TResult ExecuteRequestOn<TResult>(this NavigationSpecification<TResult> navigationSpecification, IClient client) => client.ExecuteRequest(navigationSpecification);
   public static void ExecuteRequestOn(this NavigationSpecification navigationSpecification, IClient client) => client.ExecuteRequest(navigationSpecification);
   public static async Task<TResult> ExecuteRequestOnAsync<TResult>(this NavigationSpecification<TResult> navigationSpecification, IClient client) => await client.ExecuteRequestAsync(navigationSpecification).caf();
   public static async Task ExecuteRequestOnAsync(this NavigationSpecification navigationSpecification, IClient client) => await client.ExecuteRequestAsync(navigationSpecification).caf();
}
