using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Internal;

public interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
