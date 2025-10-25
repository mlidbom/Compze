using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Internal;

interface ITeventStoreTeventPublisher
{
   void Publish(ITaggregateTevent aTevent);
}
