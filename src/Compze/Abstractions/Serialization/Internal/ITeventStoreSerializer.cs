using System;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Serialization.Internal;

interface ITeventStoreSerializer
{
   string Serialize(TaggregateTevent tevent);
   ITaggregateTevent Deserialize(Type teventType, string json);
}
