namespace Compze.Teventive.Taggregates.Tevents.Public;

public interface IGetTaggregateEntityTeventEntityId<in TTevent, out TEntityId>
{
   TEntityId GetId(TTevent tevent);
}