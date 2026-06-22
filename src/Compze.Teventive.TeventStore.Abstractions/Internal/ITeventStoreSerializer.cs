using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Internal;

public interface ITeventStoreSerializer
{
   string Serialize(TaggregateTevent tevent);
   ITaggregateTevent Deserialize(Type teventType, string json);
}
