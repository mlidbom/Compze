namespace Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

public interface IEventStoreEventPublisher
{
   void Publish(IAggregateEvent anEvent);
}
