using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive._internal;
using Compze.Teventive.Taggregates;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private;

///<summary>A taggregate class a state store is declared to keep, as the store works with every class alike: it builds empty<br/>
/// instances of it, takes snapshots of them, and restores them from snapshots, whatever its snapshot type.</summary>
sealed class DeclaredTaggregateClass
{
   readonly Func<ITaggregate> _createEmpty;
   readonly Func<ITaggregate, object> _takeSnapshot;
   readonly Action<ITaggregate, object> _restoreFrom;

   DeclaredTaggregateClass(Type taggregateClass, Func<ITaggregate> createEmpty, Func<ITaggregate, object> takeSnapshot, Action<ITaggregate, object> restoreFrom)
   {
      Class = taggregateClass;
      _createEmpty = createEmpty;
      _takeSnapshot = takeSnapshot;
      _restoreFrom = restoreFrom;
   }

   internal static DeclaredTaggregateClass For<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> =>
      new(typeof(TTaggregate),
          () => (TTaggregate)createEmpty(),
          taggregate => ((TTaggregate)taggregate).TakeSnapshot()._assert().NotNull(),
          (taggregate, snapshot) => ((TTaggregate)taggregate).RestoreFrom((TSnapshot)snapshot));

   ///<summary>The class itself: a taggregate is of this exact class, never of a subclass of it.</summary>
   internal Type Class { get; }

   internal object TakeSnapshot(ITaggregate taggregate) => _takeSnapshot(taggregate);

   ///<summary>A new instance, holding the state <paramref name="snapshot"/> holds of the taggregate <paramref name="id"/>.</summary>
   internal ITaggregate Restore(TaggregateId id, object snapshot)
   {
      var restored = _createEmpty();
      State.Assert(restored.GetType() == Class, () => $"The factory declared for {Class.FullName} built a {restored.GetType().FullName}. Declare each taggregate class with a factory of its own.");
      _restoreFrom(restored, snapshot);
      State.Assert(restored.Version > 0, () => $"{Class.FullName}.{nameof(ISnapshotable<,>.RestoreFrom)} gave the instance no id. It must call RestoreIdentity first.");
      State.Assert(restored.Id == id, () => $"{Class.FullName}.{nameof(ISnapshotable<,>.RestoreFrom)} restored taggregate {restored.Id} from a snapshot of taggregate {id}.");
      return restored;
   }

   ///<summary>Whether <paramref name="held"/> and <paramref name="loaded"/>, two instances of one taggregate, hold different states:<br/>
   /// whether their snapshots differ, by the snapshot type's own equality.</summary>
   internal bool StatesDiffer(ITaggregate held, ITaggregate loaded)
   {
      if(held.GetType() != loaded.GetType()) return true;
      var heldSnapshot = TakeSnapshot(held);
      State.Assert(HasValueEquality(heldSnapshot.GetType()),
                   () => $"{heldSnapshot.GetType().FullName}, the snapshot type of {Class.FullName}, doesn't override Equals, so a reload can't tell whether the state of a {Class.Name} changed. Give it value equality.");
      return !heldSnapshot.Equals(TakeSnapshot(loaded));

      static bool HasValueEquality(Type snapshotType) => snapshotType.GetMethod(nameof(Equals), [typeof(object)])!.DeclaringType != typeof(object);
   }

   ///<summary>A private copy of <paramref name="committed"/> for one unit of work, made through a snapshot: an instance of its own,<br/>
   /// with the same state and version.</summary>
   internal ITaggregate CopyOf(ITaggregate committed)
   {
      var copy = Restore(committed.Id, TakeSnapshot(committed));
      ((ITaggregateStoreInternals)copy).ContinueVersion(committed.Version);
      return copy;
   }
}
