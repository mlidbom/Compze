using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive;

public interface ITaggregate : ITentity
{
   EntityId IEntity.Id => Id;
   new TaggregateId Id { get; }
   int Version { get; }

   ///<summary>Hands the not-yet-persisted tevents to <paramref name="commitTevents"/> and forgets them: exactly the wrapped tevents publishing created,<br/>
   /// each inside its publisher's <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper, so no publisher identity is lost on the way to the store.</summary>
   ///<remarks>todo: reconsider whether this method should exist (Magnus, 2026-11-16). A store gets a taggregate's tevents two ways:<br/>
   /// from <see cref="TeventStream"/> for one it loaded, and from this method for a new one, whose creation tevents were published<br/>
   /// before any store knew it, on a stream nobody listened to. Calling a store back from in here is what let a handler's tevent be<br/>
   /// lost (dev_docs/taggregate-improvements/shared/bugs-and-findings.md). One way in may be cleaner: the stream hands the tevents<br/>
   /// not yet handed to a store to whoever starts tracking the taggregate, so a store subscribes at its save as it does at a load,<br/>
   /// and this method goes.</remarks>
   void Commit(Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> commitTevents);
   ///<summary>Rebuilds this taggregate's state by applying its persisted history: the wrapped tevents exactly as the store loaded them.<br/>
   /// The stored wrapper is what gets applied - after a migration has rewritten history, the stored wrapper is the truth, not what this taggregate would wrap today.</summary>
   void LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history);
   ///<summary>Every tevent this taggregate publishes, as published: inside its publisher's <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper.</summary>
   IObservable<ITaggregateTevent<ITaggregateTevent>> TeventStream { get; }
}

public interface ITaggregate<out TTevent> : ITaggregate where TTevent : ITaggregateTevent
{
   new IObservable<ITaggregateTevent<TTevent>> TeventStream { get; }
}