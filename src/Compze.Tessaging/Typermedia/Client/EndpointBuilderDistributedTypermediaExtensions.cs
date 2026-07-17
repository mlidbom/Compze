using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Typermedia.Client;

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

public static class EndpointFoundationDistributedTypermediaExtensions
{
   extension(EndpointFoundation @this)
   {
      ///<summary>Adds distributed Typermedia to a composed endpoint (<see cref="EndpointFoundation"/>): runs <paramref name="compose"/><br/>
      /// to fill the feature's slots (e.g. the serializer), then adds the feature. Typermedia persists nothing, so unlike distributed<br/>
      /// Tessaging it needs no database on the foundation.</summary>
      public DistributedTypermediaEndpointFeature AddDistributedTypermedia(Action<DistributedTypermediaComposition> compose)
      {
         compose(new DistributedTypermediaComposition(@this.Builder.Registrar));
         return @this.Builder.AddDistributedTypermedia();
      }
   }
}

public static class EndpointTypermediaExtensions
{
   extension(IEndpoint @this)
   {
      ///<summary>The address where this endpoint listens for Typermedia — the endpoint's one transport-server address, which serves every<br/>
      /// distributed capability the endpoint speaks. Null until the endpoint is listening, and for endpoints without the distributed pipeline<br/>
      /// (the distributed substrate is the Tessaging core's, which distributed Typermedia composes).</summary>
      public EndpointAddress? TypermediaAddress => @this.Components.OfType<Compze.Tessaging.Hosting.DistributedTessagingEndpointComponent>().SingleOrDefault()?.Address;
   }
}
