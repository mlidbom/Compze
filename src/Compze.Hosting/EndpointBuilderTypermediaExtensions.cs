using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Hosting;

public static class EndpointBuilderTypermediaExtensions
{
   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterTypermediaHandlers(this IEndpointBuilder @this)
      => ((ServerEndpointBuilder)@this).RegisterTypermediaHandlers;
}
