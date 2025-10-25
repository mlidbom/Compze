using System;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IEventStoreSerializer
{
   string Serialize(AggregateEvent @event);
   IAggregateEvent Deserialize(Type eventType, string json);
}
