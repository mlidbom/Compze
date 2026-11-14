using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging.Endpoints;

///<summary>Thrown to a caller whose request to an endpoint was never sent, because every connection its sender has to the<br/>
/// endpoint stayed busy for as long as the request waited for one, each awaiting the answer to an earlier request: the<br/>
/// endpoint is not answering as fast as the sender sends to it. The sender is the endpoint, or the pure client, that the<br/>
/// request is sent from.</summary>
///<remarks>Public because it reaches the code that converses with the endpoint, such as a typermedia navigator's caller,<br/>
/// and that code can recover from it: by sending less at once, by trying again later, or by going on without the answer.<br/>
/// What tells it from an <see cref="EndpointUnreachableException"/> is that the endpoint is reached: the connections to it<br/>
/// are open, and in use.</remarks>
///<remarks>Thrown by the transports whose sender holds a bounded number of connections to each endpoint: named pipes and<br/>
/// TCP. A request holds its connection until its answer has arrived, the endpoint's handling of it included, so the bound<br/>
/// is also how many requests a sender has awaiting answers from one endpoint at once. The HTTP transport bounds neither,<br/>
/// and never throws it.</remarks>
public class EndpointBusyException : Exception
{
   EndpointBusyException(string message) : base(message) {}

   ///<summary>The <paramref name="request"/> waited <paramref name="timeout"/> for one of the sender's<br/>
   /// <paramref name="connectionsToTheEndpoint"/> connections to the endpoint to become free, and none did.</summary>
   ///<remarks>The message carries the thread pool's state: connections that stay busy against an endpoint that is keeping<br/>
   /// up are the signature of a thread pool not servicing continuations, and zero free workers with work pending says so.<br/>
   /// A healthy pool says that the endpoint itself is what is slow.</remarks>
   internal static EndpointBusyException BecauseNoConnectionBecameFreeInTime(string transportName, EndpointAddress address, TransportRequest request, int connectionsToTheEndpoint, TimeSpan timeout) => new(
      $"""
       The endpoint at {address} is busy: all {connectionsToTheEndpoint} {transportName} connections the sender has to it stayed busy for {timeout}, each awaiting the answer to an earlier request. The request was never sent.
       Kind:       {request.Kind}
       Type:       {request.PayloadTypeIdString}
       A request holds its connection until its answer has arrived, the endpoint's handling of it included, so connections that stay busy mean that the endpoint's handlers are not keeping up with what the sender sends to it.
       ThreadPool at timeout: {ThreadPoolCE.DescribeState()}
       """);
}
