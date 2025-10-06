namespace Compze.EventStore.Abstractions;

interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
