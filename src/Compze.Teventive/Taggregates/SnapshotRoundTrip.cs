using Compze.Teventive.Taggregates._internal;
using Compze.Teventive.Taggregates._private;

namespace Compze.Teventive.Taggregates;

///<summary>The check, for a consumer's specifications, that a taggregate's snapshot loses nothing: that a copy made through it holds<br/>
/// the state the taggregate holds, in the taggregate itself and in every part of it.</summary>
///<remarks>A store that keeps taggregates as their state makes each unit of work's private copy of a taggregate through a snapshot, so<br/>
/// whatever the snapshot misses is lost at the next change, and saved lost. Three mistakes lose state silently: a field the snapshot<br/>
/// type has no place for, one that <see cref="ISnapshotable{TSelf,TSnapshot}.TakeSnapshot"/> leaves out, and one that<br/>
/// <see cref="ISnapshotable{TSelf,TSnapshot}.RestoreFrom"/> leaves out. Comparing a snapshot of the copy with the snapshot it was made<br/>
/// from catches only the last, so the check compares the taggregate and the copy themselves.</remarks>
///<remarks>The state compared is the value of every field of the taggregate's own classes, and of every object reached from them:<br/>
/// field by field, and item by item, in order for lists and arrays and in any order for sets and dictionaries. Strings, numbers and<br/>
/// .NET's other simple values are compared by their own equality. An object that both refer to, such as a service the taggregates are<br/>
/// built with, is the same state. A value created on its first read, a <see cref="Compze.Internals.SystemCE.LazyCE{TValue}"/> or a<br/>
/// <see cref="Lazy{T}"/>, is compared by its value, read in both. The fields of Compze's base classes, which publish and apply the<br/>
/// tevents, aren't state, nor are delegates. The check then compares the snapshots of the two, as a state store's reload does to tell<br/>
/// what changed.</remarks>
public static class SnapshotRoundTrip
{
   ///<summary>Throws, naming each difference, unless a copy of <paramref name="taggregate"/> made through a snapshot, as a state store<br/>
   /// makes a unit of work's copy, holds the state <paramref name="taggregate"/> holds, and a snapshot of the copy equals one of<br/>
   /// <paramref name="taggregate"/>.</summary>
   ///<param name="taggregate">An instance of the class to check, with state in every field it has, and in every part.</param>
   ///<param name="createEmpty">Builds a new, empty instance of the class, as the store that keeps it is told to.</param>
   ///<param name="snapshotsAreEqual">The comparison of the class's snapshots, as declared with the class to the store. Without it, the<br/>
   /// snapshot type's own Equals compares them, and must override <see cref="object.Equals(object)"/>.</param>
   ///<remarks>review-naming: SnapshotRoundTrip.AssertLosesNothing is a placeholder until the naming pass.</remarks>
   public static void AssertLosesNothing<TTaggregate, TSnapshot>(TTaggregate taggregate,
                                                                Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty,
                                                                Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>
   {
      var differences = DifferencesOf(taggregate, createEmpty, snapshotsAreEqual);
      if(differences.Count > 0) throw new SnapshotRoundTripFailedException(typeof(TTaggregate), differences);
   }

   ///<summary>Each difference that <see cref="AssertLosesNothing{TTaggregate,TSnapshot}"/> finds, as a line naming its path from the<br/>
   /// taggregate, and none when the snapshot loses nothing: for a tool that checks many taggregates and reports what it finds.</summary>
   ///<remarks>review-naming: SnapshotRoundTrip.DifferencesOf is a placeholder until the naming pass.</remarks>
   public static IReadOnlyList<string> DifferencesOf<TTaggregate, TSnapshot>(TTaggregate taggregate,
                                                                             Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty,
                                                                             Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>
   {
      var taggregateClass = SnapshotableTaggregateClass.For(createEmpty, snapshotsAreEqual);
      var copy = taggregateClass.CopyOf(taggregate);

      var differences = TaggregateStateComparison.DifferencesBetween(taggregate, copy);
      return taggregateClass.StatesDiffer(taggregate, copy)
                ? [..differences, $"A snapshot of the copy doesn't equal a snapshot of the original, by {DescriptionOfTheComparison()}, so a state store's reload would take every {typeof(TTaggregate).Name} for changed."]
                : differences;

      string DescriptionOfTheComparison() => snapshotsAreEqual != null ? "the comparison of its snapshots given" : $"{typeof(TSnapshot).Name}'s own Equals";
   }
}
