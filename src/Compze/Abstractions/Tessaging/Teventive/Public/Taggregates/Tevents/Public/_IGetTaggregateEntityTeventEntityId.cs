namespace Compze.Tessaging.Teventive;

public interface IGetTaggregateEntityTeventEntityId<in TTevent, out TEntityId>
{
   TEntityId GetId(TTevent @tevent);
}