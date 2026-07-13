namespace Compze.Internals.Transport.NamedPipes;

///<summary>Which kind of conversation a <see cref="NamedPipeTransportRequest"/> opens — the named-pipe transport's<br/>
/// equivalent of the HTTP transport's per-kind routes. The receiving server dispatches to the handler registered for the kind.</summary>
public enum NamedPipeTransportRequestKind
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
   ///<summary>An infrastructure query (endpoint discovery et al. — see <see cref="InfrastructureQueryExecutor"/>). The response payload is the serialized query result.</summary>
   InfrastructureQuery = 6
}
