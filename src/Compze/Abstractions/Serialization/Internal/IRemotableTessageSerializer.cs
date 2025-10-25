using System;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IRemotableTessageSerializer
{
   string SerializeTessage(IRemotableTessage tessage);
   IRemotableTessage DeserializeTessage(Type tessageType, string json);

   string SerializeResponse(object response);
   object DeserializeResponse(Type responseType, string json);
}