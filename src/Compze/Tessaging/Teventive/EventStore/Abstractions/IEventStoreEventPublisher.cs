namespace Compze.Tessaging.Teventive.EventStore.Abstractions;

interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
