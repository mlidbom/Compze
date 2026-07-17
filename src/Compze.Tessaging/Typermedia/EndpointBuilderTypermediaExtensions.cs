using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Typermedia;

public static class EndpointBuilderTypermediaExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>
      /// Adds in-process Typermedia to the endpoint being built (idempotent) and returns its feature:
      /// strictly local tueries and tommands execute synchronously, on the calling thread, in the caller's
      /// session — a tommand within the caller's transaction — through the
      /// <see cref="ILocalTypermediaNavigatorSession"/>. Wires no transport server and no discovery.<br/>
      /// Handlers of every tessage kind are declared through <c>RegisterTessageHandlers</c> — on this feature
      /// or on the endpoint builder — into the endpoint's one engine.<br/>
      /// Distributed Typermedia contains this: <c>AddDistributedTypermedia()</c> (in Compze.Tessaging.Typermedia.Client)
      /// composes it.
      ///</summary>
      public InProcessTypermediaEndpointFeature AddInProcessTypermedia() => @this.GetOrAddFeature(builder => new InProcessTypermediaEndpointFeature(builder));
   }
}
