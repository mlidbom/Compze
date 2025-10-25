using System;
using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IEventStoreSerializer
{
   string Serialize(AggregateEvent @event);
   IAggregateEvent Deserialize(Type eventType, string json);
}
