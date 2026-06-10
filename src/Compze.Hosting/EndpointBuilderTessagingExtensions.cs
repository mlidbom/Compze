using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

namespace Compze.Hosting;

public static class EndpointBuilderTessagingExtensions
{
   extension(IEndpointBuilder @this)
   {
      public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers => ((ServerEndpointBuilder)@this).RegisterTessagingHandlers;
   }
}
