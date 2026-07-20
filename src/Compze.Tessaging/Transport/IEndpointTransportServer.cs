using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Transport;

///<summary>The one transport server through which an endpoint listens: whatever the endpoint speaks — Tessaging, Typermedia, or<br/>
/// both — is served through this single server, at the endpoint's single <see cref="Address"/>. Each communication style contributes<br/>
/// its request handling to the server rather than running a server of its own; the server itself answers the infrastructure<br/>
/// queries endpoint discovery runs on, which every endpoint serves no matter what it speaks.</summary>
///<remarks>One server means one address per endpoint — which is what lets an <see cref="IEndpointRegistry"/> map an<br/>
/// <see cref="EndpointId"/> to a single address, and lets every capability's discovery bootstrap through it.<br/>
/// Which transport implements the server — named pipes, ASP.NET Core — is decided by the composition: each transport registers<br/>
/// its implementation guarded (first registration wins), so every communication style's transport registration can demand a<br/>
/// server without conflicting when an endpoint hosts several styles.</remarks>
interface IEndpointTransportServer : IAsyncDisposable
{
   ///<summary>The address clients connect to while the server is listening, or <see langword="null"/> when it is not - before it<br/>
   /// has started and after it has stopped. A single atomic read, so it is safe to read concurrently with the server starting or<br/>
   /// stopping: a reader sees either the address or null, never a half-torn-down server.</summary>
   EndpointAddress? Address { get; }

   Task StartAsync();
   Task StopAsync();
}
