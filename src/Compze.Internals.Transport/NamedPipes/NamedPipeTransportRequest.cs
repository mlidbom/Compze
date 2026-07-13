using Compze.Abstractions.Public;

namespace Compze.Internals.Transport.NamedPipes;

///<summary>One request sent over the named-pipe transport: the transport-level envelope<br/>
/// (<see cref="Kind"/>, <see cref="TessageId"/>, <see cref="PayloadTypeIdString"/>) plus the serialized tessage <see cref="Body"/> —<br/>
/// the same information the HTTP transport carries in its route, headers and request body.</summary>
public class NamedPipeTransportRequest
{
   ///<summary>Which kind of conversation this request opens; the server dispatches to the handler registered for it.</summary>
   public NamedPipeTransportRequestKind Kind { get; }

   ///<summary>The envelope identity infrastructure dedups on, when the kind participates in deduplication (the exactly-once kinds and typermedia tommands).<br/>
   /// Kinds that carry no dedup identity (tueries, infrastructure queries) send a fresh id, which the receiver ignores.</summary>
   public TessageId TessageId { get; }

   ///<summary>The canonical string of the payload type's type id (<c>TypeId.CanonicalString</c>); the receiver resolves it to the .NET type to deserialize <see cref="Body"/> as.</summary>
   public string PayloadTypeIdString { get; }

   ///<summary>The serialized tessage.</summary>
   public string Body { get; }

   public NamedPipeTransportRequest(NamedPipeTransportRequestKind kind, TessageId tessageId, string payloadTypeIdString, string body)
   {
      Kind = kind;
      TessageId = tessageId;
      PayloadTypeIdString = payloadTypeIdString;
      Body = body;
   }
}
