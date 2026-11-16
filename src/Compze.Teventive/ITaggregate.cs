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
   void Commit(Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> commitTevents);
   ///<summary>Rebuilds this taggregate's state by applying its persisted history: the wrapped tevents exactly as the store loaded them.<br/>
   /// The stored wrapper is what gets applied - after a migration has rewritten history, the stored wrapper is the truth, not what this taggregate would wrap today.</summary>
   void LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history);
   ///<summary>Restores this taggregate from a snapshot of its state, without the history that led to it: the shortest history that produces the state.<br/>
   /// The snapshot starts with an <see cref="ITaggregateSnapshotTevent"/>, which restores the taggregate's own state and establishes its identity, followed by what<br/>
   /// its parts need: one created tevent for each entity, and for each shared tomponent its own tevents wrapped in its slot's adopting wrapper tevent.<br/>
   /// Each tevent is wrapped in this taggregate's own wrapper and applied by the tevent appliers only: no tevent handler or <see cref="TeventStream"/> observer<br/>
   /// sees them, and none is left uncommitted. Then the invariants are asserted.</summary>
   ///<param name="snapshot">The tevents the snapshot consists of. Restoring stamps each with this taggregate's identity and with <paramref name="version"/>.</param>
   ///<param name="version">The number of tevents the snapshot stands for: the taggregate's <see cref="Version"/> once restored. A taggregate whose history<br/>
   /// isn't kept anywhere passes 1, so that a <see cref="Version"/> above 1 means changed since it was restored.</param>
   void LoadFromSnapshot(IEnumerable<ITaggregateTevent> snapshot, int version);
   ///<summary>Every tevent this taggregate publishes, as published: inside its publisher's <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper.</summary>
   IObservable<ITaggregateTevent<ITaggregateTevent>> TeventStream { get; }
}

public interface ITaggregate<out TTevent> : ITaggregate where TTevent : ITaggregateTevent
{
   new IObservable<ITaggregateTevent<TTevent>> TeventStream { get; }
}