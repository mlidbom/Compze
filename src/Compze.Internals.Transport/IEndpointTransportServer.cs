using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Transport;

///<summary>The one transport server through which an endpoint listens: whatever the endpoint speaks — Tessaging, Typermedia, or<br/>
/// both — is served through this single server, at the endpoint's single <see cref="Address"/>. Each communication style contributes<br/>
/// its request handling to the server rather than running a server of its own; the server itself answers the infrastructure<br/>
/// queries endpoint discovery runs on, which every endpoint serves no matter what it speaks.</summary>
///<remarks>One server means one address per endpoint — which is what lets an <see cref="IEndpointRegistry"/> map an<br/>
/// <see cref="EndpointId"/> to a single address, and lets every capability's discovery bootstrap through it.<br/>
/// Which transport implements the server — named pipes, ASP.NET Core — is decided by the composition through the registered<br/>
/// <see cref="IEndpointTransportServerFactory"/>.</remarks>
public interface IEndpointTransportServer : IAsyncDisposable
{
   ///<summary>The address clients connect to. Valid once the server is listening.</summary>
   EndpointAddress Address { get; }

   Task StartAsync();
   Task StopAsync();
}

///<summary>Creates the <see cref="IEndpointTransportServer"/> for an endpoint from the endpoint's own container — the seam through<br/>
/// which the composition decides which transport the endpoint speaks. Each transport registers its factory guarded (first<br/>
/// registration wins), so every communication style's transport registration can demand a server without conflicting when an<br/>
/// endpoint hosts several styles.</summary>
///<remarks>A factory rather than a direct <see cref="IEndpointTransportServer"/> registration because the server assembles the<br/>
/// communication styles' contributions from a component set (<c>ForSet</c>), and component sets are resolved through a resolver<br/>
/// (<see cref="IServiceResolverCE.ResolveSet{TComponent}(IServiceResolver)"/>) — not injectable as a singular constructor dependency.</remarks>
public interface IEndpointTransportServerFactory
{
   ///<summary>Creates the endpoint's transport server, resolving the transport's services and the communication styles' contributions from <paramref name="endpointResolver"/>.</summary>
   IEndpointTransportServer CreateServer(IRootResolver endpointResolver);
}
