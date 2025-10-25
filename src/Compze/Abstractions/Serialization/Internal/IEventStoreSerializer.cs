using System;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface IEventStoreSerializer
{
   string Serialize(AggregateTevent tevent);
   IAggregateTevent Deserialize(Type eventType, string json);
}
