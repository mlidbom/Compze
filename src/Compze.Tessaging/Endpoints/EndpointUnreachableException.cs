using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging.Endpoints;

///<summary>Thrown to a caller whose request to an endpoint got no answer because the endpoint could not be reached: no<br/>
/// connection to its address could be made, or the connection ended before the answer arrived. The endpoint's process is<br/>
/// not running, its machine is off, paused or cut off from the network, or the address is not one it listens at.</summary>
///<remarks>Public because it reaches the code that converses with the endpoint, such as a typermedia navigator's caller,<br/>
/// and that code can recover from it: by trying again later, or by going on without the endpoint. Every transport's client<br/>
/// throws it for these failures, with the transport's own exception, where there is one, as the<br/>
/// <see cref="Exception.InnerException"/>.</remarks>
///<remarks>It does not promise that the endpoint never saw the request. When no connection could be made, it did not.<br/>
/// When the connection ended before the answer arrived, the endpoint may have received the request and handled it: the<br/>
/// message says which of the two happened.</remarks>
public class EndpointUnreachableException : Exception
{
   EndpointUnreachableException(string message, Exception? transportsOwnException) : base(message, transportsOwnException) {}

   ///<summary>Nothing answered the attempt to connect within <paramref name="timeout"/>.</summary>
   ///<remarks>The message carries the thread pool's state: a connect that times out against a server that is running is<br/>
   /// the signature of a thread pool not servicing continuations, and zero free workers with work pending says so.</remarks>
   internal static EndpointUnreachableException BecauseNoConnectionWasMadeInTime(string transportName, EndpointAddress address, TimeSpan timeout, Exception? transportsOwnException = null) => new(
      $"""
       The endpoint at {address} could not be reached: no {transportName} connection to it was made within {timeout}. The request was never sent.
       ThreadPool at timeout: {ThreadPoolCE.DescribeState()}
       """,
      transportsOwnException);

   ///<summary>The attempt to connect failed outright: the address refused it, or there is no way to the address.</summary>
   internal static EndpointUnreachableException BecauseTheConnectionAttemptFailed(string transportName, EndpointAddress address, Exception transportsOwnException) => new(
      $"The endpoint at {address} could not be reached: the attempt to make a {transportName} connection to it failed: {transportsOwnException.Message} The request was never sent.",
      transportsOwnException);

   ///<summary>The connection the <paramref name="request"/> was sent over ended before the answer to it arrived.</summary>
   internal static EndpointUnreachableException BecauseTheConnectionEndedBeforeTheAnswerArrived(string transportName, EndpointAddress address, TransportRequest request, Exception transportsOwnException) => new(
      $"""
       The endpoint at {address} could not be reached for an answer: the {transportName} connection to it ended before the answer to the request arrived: {transportsOwnException.Message}
       Whether the endpoint received the request, and handled it, is not known.
       Kind:       {request.Kind}
       Type:       {request.PayloadTypeIdString}
       """,
      transportsOwnException);

   ///<summary>The <paramref name="request"/> was sent to a transport that connects and sends in one step, and it got no<br/>
   /// response: the connection could not be made, or ended before the response arrived.</summary>
   internal static EndpointUnreachableException BecauseTheRequestGotNoResponse(string transportName, EndpointAddress address, TransportRequest request, Exception transportsOwnException) => new(
      $"""
       The endpoint at {address} could not be reached: the {transportName} request to it got no response: {transportsOwnException.Message}
       Whether the endpoint received the request, and handled it, is not known.
       Kind:       {request.Kind}
       Type:       {request.PayloadTypeIdString}
       """,
      transportsOwnException);
}
