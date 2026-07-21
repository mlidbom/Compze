using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.NamedPipes;

///<summary>The named-pipe implementation of <see cref="IEndpointTransportClient"/>: sends each request through<br/>
/// <see cref="NamedPipeTransportClient"/> to the endpoint's named-pipe transport server (<see cref="NamedPipeEndpointTransportServer"/>).</summary>
class NamedPipeEndpointTransportClient : IEndpointTransportClient
{
   public async Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default) =>
      await NamedPipeTransportClient.SendAsync(request, address, cancellationToken).caf();
}
