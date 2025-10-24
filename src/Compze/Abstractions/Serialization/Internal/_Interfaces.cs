using System;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IJsonSerializer
{
   string Serialize(object instance);
   object Deserialize(Type type, string json);
}

interface IEventStoreSerializer
{
   string Serialize(AggregateEvent @event);
   IAggregateEvent Deserialize(Type eventType, string json);
}

interface IDocumentDbSerializer
{
   string Serialize(object instance);
   object Deserialize(Type eventType, string json);
}

interface IRemotableMessageSerializer
{
   string SerializeMessage(IRemotableMessage message);
   IRemotableMessage DeserializeMessage(Type messageType, string json);

   string SerializeResponse(object response);
   object DeserializeResponse(Type responseType, string json);
}