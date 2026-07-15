using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;

namespace Compze.Typermedia.Client;

///<summary>Distributed Typermedia's runtime lifecycle within an endpoint. The serving itself is done by the endpoint's one<br/>
/// transport server (<see cref="EndpointTransportServerFeature"/>'s component), to which Typermedia contributes its request<br/>
/// handling. What this component drives is the client side: when the endpoint declared the registry it discovers other endpoints<br/>
/// through, the sending phase sets the endpoint's <see cref="ITypermediaRouter"/> reconciling against the registry's membership —<br/>
/// continuously, so endpoints that appear, disappear, or restart at a new address are followed. It is also how the endpoint<br/>
/// surface shows that distributed Typermedia is listening, and where the <c>TypermediaAddress</c> extension property reads the<br/>
/// endpoint's address.</summary>
sealed class DistributedTypermediaEndpointComponent : IEndpointComponent
{
   readonly ITypermediaRouter _typermediaRouter;
   readonly EndpointTransportServerFeature _transportServer;
   //Null when the endpoint declared no registry to discover endpoints through: it only serves, and its router never runs.
   readonly IEndpointRegistry? _endpointRegistry;

   internal DistributedTypermediaEndpointComponent(ITypermediaRouter typermediaRouter, EndpointTransportServerFeature transportServer, IEndpointRegistry? endpointRegistry)
   {
      _typermediaRouter = typermediaRouter;
      _transportServer = transportServer;
      _endpointRegistry = endpointRegistry;
   }

   ///<summary>The endpoint's one listening address (see <see cref="EndpointTransportServerFeature.ListeningAddress"/>); null until the endpoint's transport server is listening.</summary>
   internal EndpointAddress? Address => _transportServer.ListeningAddress;

   public Task StartListeningAsync() => Task.CompletedTask;

   public async Task StartSendingAsync()
   {
      if(_endpointRegistry is null) return;

      //The router converges on the registry's membership and keeps reconciling, so endpoints in other processes that appear,
      //disappear, or restart at a new address are connected, dropped, or re-connected as the registry changes. The endpoints'
      //transport servers started in the listening phase, which the host completes everywhere before any sending starts, so the
      //first reconciliation connects to every endpoint the registry already lists.
      _typermediaRouter.Start();
      await _typermediaRouter.StartMaintainingConnectionsAsync(_endpointRegistry).caf();
   }

   public Task StopSendingAsync()
   {
      if(_endpointRegistry is not null) _typermediaRouter.Stop();
      return Task.CompletedTask;
   }

   public Task StopListeningAsync() => Task.CompletedTask;
}
