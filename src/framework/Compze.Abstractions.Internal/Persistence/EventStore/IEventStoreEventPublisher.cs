using Compze.EventStore.Abstractions;

namespace Compze.Persistence.EventStore;

interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
