using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Internal.Transport;

namespace Compze.Tessaging.Private.Transport;

///<summary>The client side of the endpoint transport: sends one <see cref="TransportRequest"/> to the transport server<br/>
/// (<see cref="IEndpointTransportServer"/>) of the endpoint at an <see cref="EndpointAddress"/> and returns the response payload.<br/>
/// Every conversation with a remote endpoint — tessaging, typermedia, endpoint discovery — travels through this one client;<br/>
/// the protocol difference between named pipes and HTTP lives entirely in its implementations.</summary>
///<remarks>A handler exception on the serving side surfaces here as a <see cref="TessageDispatchingFailedException"/> carrying<br/>
/// the serving side's exception type and detail, whatever the protocol.</remarks>
interface IEndpointTransportClient
{
   Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default);
}
