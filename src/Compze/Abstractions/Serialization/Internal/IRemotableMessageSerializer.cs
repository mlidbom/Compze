using System;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IRemotableMessageSerializer
{
   string SerializeMessage(IRemotableMessage message);
   IRemotableMessage DeserializeMessage(Type messageType, string json);

   string SerializeResponse(object response);
   object DeserializeResponse(Type responseType, string json);
}