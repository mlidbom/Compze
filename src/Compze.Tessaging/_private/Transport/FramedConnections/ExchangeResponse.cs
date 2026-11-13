using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>What came back over a connection for a request: either the handler's success payload, or the exception it threw<br/>
/// (as type name + detail) for the client to rethrow. The response half of an exchange, see <see cref="ExchangeFraming"/>.</summary>
class ExchangeResponse
{
   readonly bool _succeeded;
   readonly string _payload;
   readonly string _exceptionType;
   readonly string _exceptionDetail;

   internal static ExchangeResponse Success(string payload) => new(succeeded: true, payload: payload, exceptionType: "", exceptionDetail: "");
   internal static ExchangeResponse Error(string exceptionType, string exceptionDetail) => new(succeeded: false, payload: "", exceptionType: exceptionType, exceptionDetail: exceptionDetail);

   ExchangeResponse(bool succeeded, string payload, string exceptionType, string exceptionDetail)
   {
      _succeeded = succeeded;
      _payload = payload;
      _exceptionType = exceptionType;
      _exceptionDetail = exceptionDetail;
   }

   ///<summary>The handler's payload. When the handler threw instead, throws a <see cref="TessageDispatchingFailedException"/><br/>
   /// carrying the serving side's exception type and detail, and describing the <paramref name="request"/> this response<br/>
   /// answered and the <paramref name="address"/> it was sent to.</summary>
   internal string PayloadOrThrowTheServingSidesFailure(TransportRequest request, EndpointAddress address) =>
      _succeeded
         ? _payload
         : throw new TessageDispatchingFailedException($"""
                                                        Address:    {address.Uri}
                                                        Kind:       {request.Kind}
                                                        Type:       {request.PayloadTypeIdString}
                                                        Body:
                                                        {request.Body}

                                                        Exception Type: {_exceptionType}
                                                        Exception Tessage: {_exceptionDetail}
                                                        """);
}
