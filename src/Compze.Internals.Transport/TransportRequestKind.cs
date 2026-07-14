namespace Compze.Internals.Transport;

///<summary>Which kind of conversation a <see cref="TransportRequest"/> opens — carried by the named pipes as the request's kind field,<br/>
/// and by HTTP as the per-kind route. The receiving server dispatches to the handler registered for the kind.</summary>
public enum TransportRequestKind
{
   ///<summary>An exactly-once tevent for the receiving endpoint's inbox. The response is an empty-payload acknowledgement written after the inbox has registered the tevent.</summary>
   ExactlyOnceTevent = 1,
   ///<summary>An exactly-once tommand for the receiving endpoint's inbox. The response is an empty-payload acknowledgement written after the inbox has registered the tommand.</summary>
   ExactlyOnceTommand = 2,
   ///<summary>A typermedia tuery. The response payload is the serialized tuery result.</summary>
   TypermediaTuery = 3,
   ///<summary>A typermedia tommand that returns a result. The response payload is the serialized result.</summary>
   TypermediaTommandWithResult = 4,
   ///<summary>A typermedia tommand with no result. The response is an empty-payload acknowledgement.</summary>
   TypermediaVoidTommand = 5,
   ///<summary>An endpoint-discovery query (see <see cref="EndpointDiscoveryQueryExecutor"/>). The response payload is the serialized query result.</summary>
   EndpointDiscoveryQuery = 6
}
