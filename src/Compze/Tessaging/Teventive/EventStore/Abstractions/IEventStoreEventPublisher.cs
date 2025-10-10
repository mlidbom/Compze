namespace Compze.Tessaging.Teventive.EventStore.Abstractions;

public interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
