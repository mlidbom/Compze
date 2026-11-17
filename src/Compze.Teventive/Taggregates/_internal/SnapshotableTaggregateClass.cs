using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive._internal;

namespace Compze.Teventive.Taggregates._internal;

///<summary>A taggregate class whose instances are <see cref="ISnapshotable{TSelf,TSnapshot}"/>, as code that works with every such class<br/>
/// alike sees it: it builds empty instances of the class, takes snapshots of them, restores them from snapshots, copies them<br/>
/// through one, and tells whether two hold the same state, whatever the class's snapshot type.</summary>
///<remarks>A state store holds one for each class it's declared to keep. <see cref="SnapshotRoundTrip"/> checks a class through one,<br/>
/// so that what it checks is the copy the store makes.</remarks>
sealed class SnapshotableTaggregateClass
{
   readonly Func<ITaggregate> _createEmpty;
   readonly Func<ITaggregate, object> _takeSnapshot;
   readonly Action<ITaggregate, object> _restoreFrom;
   readonly Func<object, object, bool>? _snapshotsAreEqual;

   SnapshotableTaggregateClass(Type taggregateClass,
                           Type snapshotType,
                           Func<ITaggregate> createEmpty,
                           Func<ITaggregate, object> takeSnapshot,
                           Action<ITaggregate, object> restoreFrom,
                           Func<object, object, bool>? snapshotsAreEqual)
   {
      Class = taggregateClass;
      SnapshotType = snapshotType;
      _createEmpty = createEmpty;
      _takeSnapshot = takeSnapshot;
      _restoreFrom = restoreFrom;
      _snapshotsAreEqual = snapshotsAreEqual;
   }

   internal static SnapshotableTaggregateClass For<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> =>
      new(typeof(TTaggregate),
          typeof(TSnapshot),
          () => (TTaggregate)createEmpty(),
          taggregate => ((TTaggregate)taggregate).TakeSnapshot()._assert().NotNull(),
          (taggregate, snapshot) => ((TTaggregate)taggregate).RestoreFrom((TSnapshot)snapshot),
          snapshotsAreEqual == null ? null : (one, other) => snapshotsAreEqual((TSnapshot)one, (TSnapshot)other));

   ///<summary>The class itself: a taggregate is of this exact class, never of a subclass of it.</summary>
   internal Type Class { get; }

   ///<summary>The type of the class's snapshots, which its instances take and are restored from.</summary>
   internal Type SnapshotType { get; }

   internal object TakeSnapshot(ITaggregate taggregate) => _takeSnapshot(taggregate);

   ///<summary>A new instance, holding the state <paramref name="snapshot"/> holds of the taggregate <paramref name="id"/>.</summary>
   internal ITaggregate Restore(TaggregateId id, object snapshot)
   {
      var restored = Restore(snapshot);
      State.Assert(restored.Id == id, () => $"{Class.FullName}.{nameof(ISnapshotable<,>.RestoreFrom)} restored taggregate {restored.Id} from a snapshot of taggregate {id}.");
      return restored;
   }

   ///<summary>A new instance, holding the state <paramref name="snapshot"/> holds, whichever taggregate that is: for a storage that<br/>
   /// learns a taggregate's id only from its snapshot.</summary>
   internal ITaggregate Restore(object snapshot)
   {
      var restored = _createEmpty();
      State.Assert(restored.GetType() == Class, () => $"The factory declared for {Class.FullName} built a {restored.GetType().FullName}. Declare each taggregate class with a factory of its own.");
      _restoreFrom(restored, snapshot);
      State.Assert(restored.Version > 0, () => $"{Class.FullName}.{nameof(ISnapshotable<,>.RestoreFrom)} gave the instance no id. It must call RestoreIdentity first.");
      return restored;
   }

   ///<summary>Whether <paramref name="held"/> and <paramref name="loaded"/>, two instances of one taggregate, hold different states:<br/>
   /// whether their snapshots differ, by the comparison declared with the class, or else by the snapshot type's own equality.</summary>
   internal bool StatesDiffer(ITaggregate held, ITaggregate loaded)
   {
      if(held.GetType() != loaded.GetType()) return true;
      var heldSnapshot = TakeSnapshot(held);
      var loadedSnapshot = TakeSnapshot(loaded);
      if(_snapshotsAreEqual != null) return !_snapshotsAreEqual(heldSnapshot, loadedSnapshot);

      State.Assert(HasValueEquality(heldSnapshot.GetType()),
                   () => $"{heldSnapshot.GetType().FullName}, the snapshot type of {Class.FullName}, doesn't override Equals, so a reload can't tell whether the state of a {Class.Name} changed. Override Equals, or declare the class with a comparison of its snapshots.");
      return !heldSnapshot.Equals(loadedSnapshot);

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
