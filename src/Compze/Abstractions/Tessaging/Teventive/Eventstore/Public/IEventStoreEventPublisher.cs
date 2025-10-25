namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

public interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
