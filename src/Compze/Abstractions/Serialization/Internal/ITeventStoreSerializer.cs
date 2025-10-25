using System;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Serialization.Internal;

interface ITeventStoreSerializer
{
   string Serialize(TaggregateTevent tevent);
   ITaggregateTevent Deserialize(Type teventType, string json);
}
