using Compze.Abstractions.Hosting.Public;

namespace Compze.Typermedia.Client;

public static class EndpointBuilderDistributedTypermediaExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>
      /// Adds the distributed Typermedia pipeline to the endpoint being built (idempotent) and returns its
      /// feature: everything in-process Typermedia has, plus the transport server through which remote
      /// clients execute the endpoint's remotable handlers, the handler executor serving them, and discovery.
      ///</summary>
      public DistributedTypermediaEndpointFeature AddDistributedTypermedia() => @this.GetOrAddFeature(builder => new DistributedTypermediaEndpointFeature(builder));
   }
}

public static class EndpointTypermediaExtensions
{
   extension(IEndpoint @this)
   {
      ///<summary>The address where this endpoint's typermedia transport server listens. Null until the endpoint is listening, and for endpoints without the distributed Typermedia pipeline.</summary>
      public EndpointAddress? TypermediaAddress => @this.Components.OfType<DistributedTypermediaEndpointComponent>().SingleOrDefault()?.Address;
   }
}
