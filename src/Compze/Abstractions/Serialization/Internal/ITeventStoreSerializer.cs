using System;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface ITeventStoreSerializer
{
   string Serialize(AggregateTevent tevent);
   IAggregateTevent Deserialize(Type teventType, string json);
}
