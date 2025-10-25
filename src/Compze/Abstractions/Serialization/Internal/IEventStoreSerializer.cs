using System;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IEventStoreSerializer
{
   string Serialize(AggregateEvent @event);
   IAggregateEvent Deserialize(Type eventType, string json);
}
