using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport;

namespace Compze.Typermedia.Client;

///<summary>Distributed Typermedia's runtime presence within an endpoint. The serving itself is done by the endpoint's one<br/>
/// transport server (<see cref="EndpointTransportServerFeature"/>'s component), to which Typermedia contributes its request<br/>
/// handling — so this component drives nothing; it is how the endpoint surface shows that distributed Typermedia is listening,<br/>
/// and where the <c>TypermediaAddress</c> extension property reads the endpoint's address.</summary>
sealed class DistributedTypermediaEndpointComponent : IEndpointComponent
{
   readonly EndpointTransportServerFeature _transportServer;

   internal DistributedTypermediaEndpointComponent(EndpointTransportServerFeature transportServer) => _transportServer = transportServer;

   ///<summary>The endpoint's one listening address (see <see cref="EndpointTransportServerFeature.ListeningAddress"/>); null until the endpoint's transport server is listening.</summary>
   internal EndpointAddress? Address => _transportServer.ListeningAddress;

   public Task StartListeningAsync() => Task.CompletedTask;

   public Task StopListeningAsync() => Task.CompletedTask;
}
