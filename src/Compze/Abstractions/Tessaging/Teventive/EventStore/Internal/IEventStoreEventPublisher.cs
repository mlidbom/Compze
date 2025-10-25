using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Internal;

interface IEventStoreEventPublisher
{
   void Publish(IAggregateTevent aTevent);
}
