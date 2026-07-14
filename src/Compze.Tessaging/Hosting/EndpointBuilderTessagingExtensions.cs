using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

namespace Compze.Tessaging.Hosting;

public static class EndpointBuilderTessagingExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>
      /// Adds in-process Tessaging to the endpoint being built (idempotent) and returns its feature — the
      /// style's synchronous core, which distribution composes and extends: the handler registry, the
      /// synchronous in-process tevent delivery every tevent travels, and the endpoint's one
      /// <see cref="ITeventPublisher"/>. With nothing but this feature the
      /// endpoint wires no transport, inbox, outbox, or tommand scheduler, so tevents are delivered
      /// synchronously, on the publishing thread, within the publisher's transaction, to this process's
      /// handlers.
      ///</summary>
      public InProcessTessagingEndpointFeature AddInProcessTessaging() => @this.GetOrAddFeature(builder => new InProcessTessagingEndpointFeature(builder));

      ///<summary>
      /// Adds the distributed Tessaging pipeline to the endpoint being built (idempotent) and returns its
      /// feature: everything in-process Tessaging has (<see cref="AddInProcessTessaging"/>, which it
      /// composes), plus the inbox, outbox, tommand scheduler, router and service bus session through which
      /// the endpoint converses with other endpoints.
      ///</summary>
      public DistributedTessagingEndpointFeature AddDistributedTessaging() => @this.GetOrAddFeature(builder => new DistributedTessagingEndpointFeature(builder));

      ///<summary>
      /// Registers tessaging handlers, adding in-process Tessaging (<see cref="AddInProcessTessaging"/>) to
      /// the endpoint if it is not already added. Handlers receive tevents published in-process; they are
      /// reachable from other endpoints only when the endpoint also speaks distributed Tessaging
      /// (<see cref="AddDistributedTessaging"/>).
      ///</summary>
      public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers => @this.AddInProcessTessaging().RegisterHandlers;
   }
}

public static class EndpointTessagingExtensions
{
   extension(IEndpoint @this)
   {
      ///<summary>The address where this endpoint listens for Tessaging — the endpoint's one transport-server address, which serves every<br/>
      /// distributed capability the endpoint speaks. Null until the endpoint is listening, and for endpoints without the distributed Tessaging pipeline.</summary>
      public EndpointAddress? TessagingAddress => @this.Components.OfType<DistributedTessagingEndpointComponent>().SingleOrDefault()?.Address;
   }
}
