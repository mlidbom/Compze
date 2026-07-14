using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

namespace Compze.Tessaging.Hosting;

public static class EndpointBuilderTessagingExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>
      /// Adds tessage handling to the endpoint being built (idempotent) and returns its feature: the handler
      /// registry and synchronous in-process tevent delivery — the leg every tevent travels, whatever the
      /// endpoint speaks.<br/>
      /// Declares no tevent publication mode: an endpoint whose taggregates publish through a tevent store
      /// also declares <see cref="AddInProcessTessaging"/> or <see cref="AddDistributedTessaging"/>.
      ///</summary>
      public TessageHandlingEndpointFeature AddTessageHandling() => @this.GetOrAddFeature(builder => new TessageHandlingEndpointFeature(builder));

      ///<summary>
      /// Declares the endpoint's Tessaging in-process-only (idempotent) and returns the feature: tevents are
      /// delivered synchronously, on the publishing thread, within the publisher's transaction, to this
      /// process's handlers — and nowhere else. Wires no transport, inbox, outbox, or tommand scheduler.<br/>
      /// Mutually exclusive with <see cref="AddDistributedTessaging"/>.
      ///</summary>
      public InProcessTessagingEndpointFeature AddInProcessTessaging() => @this.GetOrAddFeature(builder => new InProcessTessagingEndpointFeature(builder));

      ///<summary>
      /// Adds the distributed Tessaging pipeline to the endpoint being built (idempotent) and returns its
      /// feature: everything tessage handling has, plus the inbox, outbox, tommand scheduler, router and
      /// service bus session through which the endpoint converses with other endpoints.<br/>
      /// Mutually exclusive with <see cref="AddInProcessTessaging"/>.
      ///</summary>
      public DistributedTessagingEndpointFeature AddDistributedTessaging() => @this.GetOrAddFeature(builder => new DistributedTessagingEndpointFeature(builder));

      ///<summary>
      /// Registers tessaging handlers, adding tessage handling (<see cref="AddTessageHandling"/>) to the
      /// endpoint if it is not already added. Handlers receive tevents published in-process; they are
      /// reachable from other endpoints only when the endpoint also speaks distributed Tessaging
      /// (<see cref="AddDistributedTessaging"/>).
      ///</summary>
      public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers => @this.AddTessageHandling().RegisterHandlers;
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
