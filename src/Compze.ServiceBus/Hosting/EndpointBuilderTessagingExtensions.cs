using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

namespace Compze.ServiceBus.Hosting;

public static class EndpointBuilderTessagingExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>Adds the Tessaging pipeline to the endpoint being built (idempotent) and returns its feature.</summary>
      public TessagingEndpointFeature AddTessaging() => @this.GetOrAddFeature(builder => new TessagingEndpointFeature(builder));

      ///<summary>Registers tessaging handlers, adding the Tessaging pipeline to the endpoint if it is not already added.</summary>
      public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers => @this.AddTessaging().RegisterHandlers;
   }
}

public static class EndpointTessagingExtensions
{
   extension(IEndpoint @this)
   {
      ///<summary>The address where this endpoint's tessaging inbox listens. Null until the endpoint is listening, and for endpoints without the Tessaging pipeline.</summary>
      public EndpointAddress? TessagingAddress => @this.Components.OfType<TessagingEndpointComponent>().SingleOrDefault()?.Address;
   }
}
