namespace Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

public interface IGetTaggregateEntityTeventEntityId<in TTevent, out TEntityId>
{
   TEntityId GetId(TTevent @tevent);
}