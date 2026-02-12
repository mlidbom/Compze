using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal;

public interface ITeventStoreTeventPublisher
{
   void Publish(ITaggregateTevent aTevent);
}
