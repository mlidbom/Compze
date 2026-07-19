using Compze.Tessaging.Abstractions.Public;

namespace Compze.Tessaging.Internals.Transport;

///<summary>One request sent to an endpoint's transport server (<see cref="IEndpointTransportServer"/>): the transport-level envelope<br/>
/// (<see cref="Kind"/>, <see cref="TessageId"/>, <see cref="PayloadTypeIdString"/>) plus the serialized tessage <see cref="Body"/>.<br/>
/// The named pipes carry it as a framed message; HTTP carries the same information in its route, headers and request body.</summary>
class TransportRequest
{
   ///<summary>Which kind of conversation this request opens; the server dispatches to the handler registered for it.</summary>
   internal TransportRequestKind Kind { get; }

   ///<summary>The envelope identity infrastructure dedups on, when the kind participates in deduplication (the exactly-once kinds and typermedia tommands).<br/>
   /// Kinds that carry no dedup identity (tueries, endpoint-discovery queries) send a fresh id, which the receiver ignores.</summary>
   internal TessageId TessageId { get; }

   ///<summary>The canonical string of the payload type's type id (<c>TypeId.CanonicalString</c>); the receiver resolves it to the .NET type to deserialize <see cref="Body"/> as.</summary>
   internal string PayloadTypeIdString { get; }

   ///<summary>The serialized tessage.</summary>
   internal string Body { get; }

   internal TransportRequest(TransportRequestKind kind, TessageId tessageId, string payloadTypeIdString, string body)
   {
      Kind = kind;
      TessageId = tessageId;
      PayloadTypeIdString = payloadTypeIdString;
      Body = body;
   }
}
