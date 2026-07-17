using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.TessageHandling.Registration.Public;

namespace Compze.Tessaging.Hosting;

public static class EndpointBuilderTessagingExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>
      /// Adds in-process Tessaging to the endpoint being built (idempotent) and returns its feature — the
      /// style's synchronous core, which distribution composes and extends: the handler registry, the
      /// synchronous in-process tevent delivery every tevent travels, and the endpoint's one
      /// <see cref="IUnitOfWorkTeventPublisher"/>. With nothing but this feature the
      /// endpoint wires no transport, inbox, or outbox, so tevents are delivered
      /// synchronously, on the publishing thread, within the publisher's transaction, to this process's
      /// handlers.
      ///</summary>
      public InProcessTessagingEndpointFeature AddInProcessTessaging() => @this.GetOrAddFeature(builder => new InProcessTessagingEndpointFeature(builder));

      ///<summary>
      /// Adds guarantee-free distributed Tessaging to the endpoint being built (idempotent) and returns its
      /// feature — the transport-speaking core, which the full distributed pipeline composes and extends:
      /// everything in-process Tessaging has, plus the transport server, the router, and the best-effort tevent
      /// delivery leg. Such an endpoint converses in best-effort tevents — across the wire with no
      /// outbox, no inbox, and no database anywhere — so it composes on the database-less foundation;
      /// everything exactly-once fails loud at setup or publish.
      ///</summary>
      public DistributedTessagingEndpointFeature AddDistributedTessaging() => @this.GetOrAddFeature(builder => new DistributedTessagingEndpointFeature(builder));

      ///<summary>
      /// Adds the full exactly-once Tessaging pipeline to the endpoint being built (idempotent) and returns its
      /// feature: everything distributed Tessaging has (<see cref="AddDistributedTessaging"/>, which it
      /// composes), plus the exactly-once vertical — inbox, outbox, the durable peer registry, and the
      /// tommand senders — through which the endpoint converses with delivery guarantees.
      ///</summary>
      public ExactlyOnceTessagingEndpointFeature AddExactlyOnceTessaging() => @this.GetOrAddFeature(builder => new ExactlyOnceTessagingEndpointFeature(builder));

      ///<summary>
      /// Registers tessaging handlers, adding in-process Tessaging (<see cref="AddInProcessTessaging"/>) to
      /// the endpoint if it is not already added. Handlers receive tevents published in-process; they are
      /// reachable from other endpoints only when the endpoint also speaks Tessaging across the wire
      /// (<see cref="AddDistributedTessaging"/> / <see cref="EndpointBuilderTessagingExtensions.AddExactlyOnceTessaging"/>).
      ///</summary>
      public ITessageHandlerRegistrar RegisterTessagingHandlers => @this.AddInProcessTessaging().RegisterHandlers;

      ///<summary>
      /// Registers transaction-ignoring tevent handlers — observation, the one subscription-side opt-down
      /// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>) — adding in-process Tessaging
      /// (<see cref="AddInProcessTessaging"/>) to the endpoint if it is not already added. Such a handler
      /// fires once, immediately, when a matching tevent is published locally or arrives from another
      /// endpoint: outside any transaction, undeterred by the fate of the transaction the tevent was
      /// published or is processed in, with no retry — a throwing handler is reported through the
      /// background-exception reporter and that delivery is over.
      ///</summary>
      public ITransactionIgnoringTeventHandlerRegistrar RegisterTransactionIgnoringTeventHandlers => @this.AddInProcessTessaging().RegisterTransactionIgnoringTeventHandlers;
   }
}

public static class EndpointFoundationDistributedTessagingExtensions
{
   extension(EndpointFoundation @this)
   {
      ///<summary>Adds guarantee-free distributed Tessaging to a composed endpoint (<see cref="EndpointFoundation"/>): runs<br/>
      /// <paramref name="compose"/> to fill the feature's slots (e.g. the serializer), then adds the feature. Distributed Tessaging<br/>
      /// persists nothing, so unlike exactly-once Tessaging it needs no database on the foundation — this is the Tessaging an<br/>
      /// endpoint whose foundation declares no database speaks.</summary>
      public DistributedTessagingEndpointFeature AddDistributedTessaging(Action<DistributedTessagingComposition> compose)
      {
         compose(new DistributedTessagingComposition(@this.Builder.Registrar));
         return @this.Builder.AddDistributedTessaging();
      }
   }
}

public static class EndpointTessagingExtensions
{
   extension(IEndpoint @this)
   {
      ///<summary>The address where this endpoint listens for Tessaging — the endpoint's one transport-server address, which serves every<br/>
      /// distributed capability the endpoint speaks. Null until the endpoint is listening, and for endpoints without transport-speaking Tessaging.</summary>
      public EndpointAddress? TessagingAddress => @this.Components.OfType<DistributedTessagingEndpointComponent>().SingleOrDefault()?.Address;
   }
}
