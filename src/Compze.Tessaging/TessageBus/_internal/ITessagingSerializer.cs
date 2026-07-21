using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus._internal;

///<summary>The Tessaging pipeline's own serializer: the format of every <see cref="ITessage"/> the pipeline sends and<br/>
/// receives. That format is both the wire body and what the inbox/outbox store, because the outbox persists the wire-ready body<br/>
/// to guarantee exactly-once delivery.</summary>
///<remarks>Each conversation protocol serializes independently: Typermedia through <see cref="Compze.Tessaging.Typermedia._internal.ITypermediaSerializer"/>, and endpoint<br/>
/// discovery through its fixed framework-internal format. What must agree is the sender's and receiver's serializer per protocol,<br/>
/// across processes — not the protocols' serializers with each other.</remarks>
interface ITessagingSerializer
{
   string SerializeTessage(ITessage tessage);
   ITessage DeserializeTessage(Type tessageType, string json);
}
