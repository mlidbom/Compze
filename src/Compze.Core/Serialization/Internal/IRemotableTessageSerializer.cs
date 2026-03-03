using Compze.Core.Tessaging.Public;

namespace Compze.Core.Serialization.Internal;

public interface IRemotableTessageSerializer
{
   string SerializeTessage(IRemotableTessage tessage);
   IRemotableTessage DeserializeTessage(Type tessageType, string json);

   string SerializeResponse(object response);
   TResponse DeserializeResponse<TResponse>(string json);
}