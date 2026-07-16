using Compze.Abstractions.Hosting.Public;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia;

public static class EndpointBuilderTypermediaExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>
      /// Adds in-process Typermedia to the endpoint being built (idempotent) and returns its feature:
      /// strictly local tueries and tommands execute synchronously, on the calling thread, in the caller's
      /// session — a tommand within the caller's transaction — through the
      /// <see cref="ISessionLocalTypermediaNavigator"/>. Wires no transport server and no discovery.<br/>
      /// Distributed Typermedia contains this: <c>AddDistributedTypermedia()</c> (in Compze.Typermedia.Client)
      /// composes it.
      ///</summary>
      public InProcessTypermediaEndpointFeature AddInProcessTypermedia() => @this.GetOrAddFeature(builder => new InProcessTypermediaEndpointFeature(builder));

      ///<summary>
      /// Registers typermedia handlers, adding in-process Typermedia (<see cref="AddInProcessTypermedia"/>)
      /// to the endpoint if it is not already added. Handlers for strictly local tueries and tommands execute
      /// in-process; remotable handlers are reachable from other applications only when the endpoint also
      /// speaks distributed Typermedia (<c>AddDistributedTypermedia()</c>, in Compze.Typermedia.Client).
      ///</summary>
      public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterTypermediaHandlers => @this.AddInProcessTypermedia().RegisterHandlers;
   }
}
