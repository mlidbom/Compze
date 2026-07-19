using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Serialization.Internal;

///<summary>The Typermedia feature's own serializer: the format of the Typermedia conversation — the tueries and tommands<br/>
/// (<see cref="ITypermediaTessage"/>) a client sends, and the results the serving endpoint returns.</summary>
///<remarks>Each conversation protocol serializes independently: Tessaging through <see cref="ITessagingSerializer"/>, and<br/>
/// endpoint discovery through its fixed framework-internal format. What must agree is the sender's and receiver's serializer per<br/>
/// protocol, across processes — not the protocols' serializers with each other.</remarks>
public interface ITypermediaSerializer
{
   string SerializeTessage(ITypermediaTessage tessage);
   ITypermediaTessage DeserializeTessage(Type tessageType, string json);

   string SerializeResult(object result);
   TResult DeserializeResult<TResult>(string json);
}
