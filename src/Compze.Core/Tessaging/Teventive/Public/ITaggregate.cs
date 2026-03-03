using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.Public;

public interface ITaggregate : ITentity
{
   EntityId IEntity.Id => Id;
   new TaggregateId Id { get; }
   int Version { get; }

   void Commit(Action<IReadOnlyList<ITaggregateTevent>> commitTevents);
   void LoadFromHistory(IEnumerable<ITaggregateTevent> history);
   IObservable<ITaggregateTevent> TeventStream { get; }
}

public interface ITaggregate<out TTevent> : ITaggregate where TTevent : ITaggregateTevent
{
   new IObservable<TTevent> TeventStream { get; }
}