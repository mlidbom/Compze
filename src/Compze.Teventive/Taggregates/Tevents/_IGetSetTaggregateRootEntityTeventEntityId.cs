namespace Compze.Teventive.Taggregates.Tevents;

//Refactor: Consider removing this interface and having the taggregate component|entity pass actions as a constructor arguments to its base class instead.
public interface IGetSetTaggregateEntityTeventEntityId<TEntityId, in TTeventImplementation, in TTevent> : IGetTaggregateEntityTeventEntityId<TTevent, TEntityId>
{
   void SetEntityId(TTeventImplementation tevent, TEntityId id);
}