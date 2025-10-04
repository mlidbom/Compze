using Compze.EventStore.Abstractions;

namespace Compze.Abstractions.Internal.Persistence.EventStore;

interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
