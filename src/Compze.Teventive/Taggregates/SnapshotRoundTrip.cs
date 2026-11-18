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
/// tevents, aren't state, nor are delegates.</remarks>
public static class SnapshotRoundTrip
{
   ///<summary>Each difference between <paramref name="taggregate"/> and a copy of it made through a snapshot, as a state store makes a<br/>
   /// unit of work's copy: a line naming its path from the taggregate, and its value in each. None when the snapshot loses nothing,<br/>
   /// so a specification asserts that the list is empty, and a tool that checks many taggregates reports what it finds.</summary>
   ///<param name="taggregate">An instance of the class to check, with state in every field it has, and in every part.</param>
   ///<param name="createEmpty">Builds a new, empty instance of the class, as the store that keeps it is told to.</param>
   ///<remarks>review-naming: SnapshotRoundTrip.DifferencesOf is a placeholder until the naming pass.</remarks>
   public static IReadOnlyList<string> DifferencesOf<TTaggregate, TSnapshot>(TTaggregate taggregate,
                                                                             Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> =>
      TaggregateStateComparison.DifferencesBetween(taggregate, SnapshotableTaggregateClass.For(createEmpty).CopyOf(taggregate));
}
