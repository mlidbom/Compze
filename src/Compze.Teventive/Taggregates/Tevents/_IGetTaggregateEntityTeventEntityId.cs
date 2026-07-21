namespace Compze.Teventive.Taggregates.Tevents;

public interface IGetTaggregateEntityTeventEntityId<in TTevent, out TEntityId>
{
   TEntityId GetId(TTevent tevent);
}