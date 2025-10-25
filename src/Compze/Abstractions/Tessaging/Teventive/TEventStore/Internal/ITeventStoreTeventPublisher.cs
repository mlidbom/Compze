using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Internal;

interface ITeventStoreTeventPublisher
{
   void Publish(ITaggregateTevent aTevent);
}
