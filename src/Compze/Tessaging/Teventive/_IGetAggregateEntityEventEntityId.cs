namespace Compze.Teventive;

public interface IGetAggregateEntityEventEntityId<in TEvent, out TEntityId>
{
   TEntityId GetId(TEvent @event);
}