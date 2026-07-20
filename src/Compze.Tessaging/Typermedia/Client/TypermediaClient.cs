using Compze.Tessaging.Typermedia.Client.Internal;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>
/// The pure client — the third composed shape beside the two endpoint types: a navigator and a transport client with no
/// server. An external client application navigates one or more endpoints' typermedia at explicitly known addresses
/// (<see cref="ConnectAsync"/>): it serves nothing, is discovered by nobody, and participates in no registry — request/response
/// through <see cref="Navigator"/> is all it does.
///
/// Composed through <see cref="Build"/>, in its own container: the client declares its transport-client protocol, its
/// serializer, and its own type mappings — the mirror of what the endpoints it navigates map — exactly as a production client
/// application deploys.
///</summary>
public class TypermediaClient : IAsyncDisposable
{
   readonly IDependencyInjectionContainer _container;
   readonly ITypermediaClientRouter _typermediaClientRouter;

   ///<summary>The door through which the client navigates the connected endpoints' typermedia.</summary>
   public IRemoteTypermediaNavigator Navigator { get; }

   internal TypermediaClient(IDependencyInjectionContainer container)
   {
      _container = container;
      _typermediaClientRouter = container.Resolve<ITypermediaClientRouter>();
      Navigator = container.Resolve<IRemoteTypermediaNavigator>();
      _typermediaClientRouter.Start();
   }

   ///<summary>Composes a pure client: runs <paramref name="build"/> over the client's declaration surface<br/>
   /// (<see cref="TypermediaClientBuilder"/>), builds the client's container, and returns the client, ready to<br/>
   /// <see cref="ConnectAsync"/> to the endpoints it knows.</summary>
   public static TypermediaClient Build(IContainerBuilder containerBuilder, Action<TypermediaClientBuilder> build)
   {
      var builder = new TypermediaClientBuilder(containerBuilder);
      build(builder);
      return builder.Build();
   }

   ///<summary>Connects to the one endpoint listening at <paramref name="endpointAddress"/> and registers routes for the<br/>
   /// typermedia types it advertises. Call once per known endpoint — a client may navigate several.</summary>
   internal async Task ConnectAsync(EndpointAddress endpointAddress) => await _typermediaClientRouter.ConnectAsync(endpointAddress).caf();

   public async ValueTask DisposeAsync()
   {
      _typermediaClientRouter.Stop();
      await _container.DisposeAsync().caf();
   }
}
