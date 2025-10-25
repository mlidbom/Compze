namespace Compze.Tessaging.Teventive;

public interface IGetAggregateEntityTeventEntityId<in TTevent, out TEntityId>
{
   TEntityId GetId(TTevent @tevent);
}