namespace Compze.Abstractions.Hosting.Public;

public static class EndpointBuilderCompositionExtensions
{
   extension(IEndpointBuilder @this)
   {
      ///<summary>Composes the endpoint's foundation — the declarations everything else builds on. Inside <paramref name="compose"/>,<br/>
      /// declare the endpoint's transport protocol first (e.g. <c>NamedPipeEndpointTransport()</c>), then — when the endpoint<br/>
      /// persists — its database (e.g. <c>SqliteEndpointDatabase("MyEndpoint")</c>). The returned <see cref="EndpointFoundation"/><br/>
      /// is what the distributed features are added on top of (e.g. <c>AddExactlyOnceTessaging(...)</c>).</summary>
      public TFoundation ComposeEndpoint<TFoundation>(Func<EndpointComposer, TFoundation> compose) where TFoundation : EndpointFoundation =>
         compose(new EndpointComposer(@this));
   }
}

///<summary>What <see cref="EndpointBuilderCompositionExtensions.ComposeEndpoint{TFoundation}"/>'s lambda receives: the surface on<br/>
/// which an endpoint's foundation is declared. The transport-protocol packages contribute the protocol declarations<br/>
/// (e.g. <c>NamedPipeEndpointTransport()</c>), each returning the <see cref="EndpointFoundation"/> the composition continues on.</summary>
public class EndpointComposer
{
   ///<summary>The endpoint being composed; the foundation declarations register into it.</summary>
   public IEndpointBuilder Builder { get; }

   internal EndpointComposer(IEndpointBuilder builder) => Builder = builder;
}

///<summary>An endpoint's declared foundation: its transport protocol — and, as <see cref="EndpointFoundation{TEndpointDatabase}"/>,<br/>
/// its database. The distributed features are added on top of it (e.g. <c>AddExactlyOnceTessaging(...)</c>), and the compiler routes<br/>
/// each feature's database-engine pairing through the foundation's type: adding exactly-once Tessaging to a foundation whose database<br/>
/// is Sqlite registers Tessaging's Sqlite sql layers, and a mismatched pairing does not compile.</summary>
public class EndpointFoundation
{
   ///<summary>The endpoint being composed; the features added on this foundation register into it.</summary>
   public IEndpointBuilder Builder { get; }

   public EndpointFoundation(IEndpointBuilder builder) => Builder = builder;
}

///<summary>An <see cref="EndpointFoundation"/> whose database is declared: <typeparamref name="TEndpointDatabase"/> names the engine<br/>
/// (e.g. <c>SqliteEndpointDatabase</c>), and the features added on the foundation bind their sql layers to that engine through it.</summary>
public class EndpointFoundation<TEndpointDatabase> : EndpointFoundation
{
   ///<summary>The endpoint's declared database.</summary>
   public TEndpointDatabase Database { get; }

   public EndpointFoundation(IEndpointBuilder builder, TEndpointDatabase database) : base(builder) => Database = database;
}
