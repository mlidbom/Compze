using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Serialization.Internal;

public interface IRemotableTessageSerializer
{
   string SerializeTessage(IMessage tessage);
   IMessage DeserializeTessage(Type tessageType, string json);

   string SerializeResponse(object response);
   TResponse DeserializeResponse<TResponse>(string json);
}