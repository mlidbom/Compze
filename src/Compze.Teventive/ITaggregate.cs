using Compze.Abstractions.Public;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive;

public interface ITaggregate : ITentity
{
   EntityId IEntity.Id => Id;
   new TaggregateId Id { get; }
   int Version { get; }

   ///<summary>Hands the not-yet-persisted tevents to <paramref name="commitTevents"/> and forgets them: exactly the wrapped tevents publishing created,<br/>
   /// each inside its publisher's <see cref="ITaggregateIdentifyingTevent{TTeventInterface}"/> wrapper, so no publisher identity is lost on the way to the store.</summary>
   void Commit(Action<IReadOnlyList<ITaggregateIdentifyingTevent<ITaggregateTevent>>> commitTevents);
   ///<summary>Rebuilds this taggregate's state by applying its persisted history: the wrapped tevents exactly as the store loaded them.<br/>
   /// The stored wrapper is what gets applied - after a migration has rewritten history, the stored wrapper is the truth, not what this taggregate would wrap today.</summary>
   void LoadFromHistory(IEnumerable<ITaggregateIdentifyingTevent<ITaggregateTevent>> history);
   ///<summary>Every tevent this taggregate publishes, as published: inside its publisher's <see cref="ITaggregateIdentifyingTevent{TTeventInterface}"/> wrapper.</summary>
   IObservable<ITaggregateIdentifyingTevent<ITaggregateTevent>> TeventStream { get; }
}

public interface ITaggregate<out TTevent> : ITaggregate where TTevent : ITaggregateTevent
{
   new IObservable<ITaggregateIdentifyingTevent<TTevent>> TeventStream { get; }
}