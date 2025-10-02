namespace Compze.Persistence.EventStore;

interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
