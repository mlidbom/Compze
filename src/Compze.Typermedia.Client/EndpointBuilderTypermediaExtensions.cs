using Compze.Abstractions.Hosting.Public;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia.Client;

public static class EndpointBuilderTypermediaExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>Adds the Typermedia pipeline to the endpoint being built (idempotent) and returns its feature.</summary>
      public TypermediaEndpointFeature AddTypermedia() => @this.GetOrAddFeature(builder => new TypermediaEndpointFeature(builder));

      ///<summary>Registers typermedia handlers, adding the Typermedia pipeline to the endpoint if it is not already added.</summary>
      public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterTypermediaHandlers => @this.AddTypermedia().RegisterHandlers;
   }
}

public static class EndpointTypermediaExtensions
{
   extension(IEndpoint @this)
   {
      ///<summary>The address where this endpoint's typermedia transport server listens. Null until the endpoint is listening, and for endpoints without the Typermedia pipeline.</summary>
      public EndpointAddress? TypermediaAddress => @this.Components.OfType<TypermediaEndpointComponent>().SingleOrDefault()?.Address;
   }
}
