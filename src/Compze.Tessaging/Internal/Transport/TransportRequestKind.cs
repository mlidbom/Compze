using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.Tessaging.Private.Transport.Advertisement;

namespace Compze.Tessaging.Internal.Transport;

///<summary>Which kind of conversation a <see cref="TransportRequest"/> opens — carried by the named pipes as the request's kind field,<br/>
/// and by HTTP as the per-kind route. The receiving server dispatches to the handler registered for the kind.</summary>
enum TransportRequestKind
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
   ///<summary>An endpoint-discovery query (see <see cref="EndpointInformationQueryExecutor"/>). The response payload is the serialized query result.</summary>
   EndpointDiscoveryQuery = 6,
   ///<summary>A best-effort tevent — a remotable tevent whose type declares no exactly-once guarantee — dispatched directly to the receiving<br/>
   /// endpoint's handlers: no inbox, no dedup, no retry. The response is an empty-payload acknowledgement written after the handlers have<br/>
   /// executed, so one-tessage-in-flight-per-destination keeps handling in send order.</summary>
   BestEffortTevent = 7
}
