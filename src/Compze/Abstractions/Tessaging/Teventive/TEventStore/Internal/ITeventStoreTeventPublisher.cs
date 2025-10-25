using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.TEventStore.Internal;

interface ITeventStoreTeventPublisher
{
   void Publish(ITaggregateTevent aTevent);
}
