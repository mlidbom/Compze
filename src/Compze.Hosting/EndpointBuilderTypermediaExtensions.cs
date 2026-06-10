using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Hosting;

public static class EndpointBuilderTypermediaExtensions
{
   extension(IEndpointBuilder @this)
   {
      public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterTypermediaHandlers => ((ServerEndpointBuilder)@this).RegisterTypermediaHandlers;
   }
}
