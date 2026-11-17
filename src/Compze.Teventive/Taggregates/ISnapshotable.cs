namespace Compze.Teventive.Taggregates;

///<summary>A taggregate whose whole state can be taken as a snapshot, of a type of its own class's choosing, and restored from one.<br/>
/// A store that keeps taggregates as their state, not their history, stores the snapshots, and makes each unit of work's private<br/>
/// copy of a taggregate through one.</summary>
///<remarks>Restoring is into a new, empty instance, which the code that composes the store says how to build. It applies no<br/>
/// tevents: <see cref="RestoreFrom"/> sets the state directly, and first gives the instance its id with the protected<br/>
/// <c>RestoreIdentity</c> of the taggregate base class.<br/>
/// A snapshot holds the taggregate's whole state, not only what is stored: every copy is made from it, so what it misses is lost at<br/>
/// the next change.<br/>
/// It holds only what the taggregate owns. An object several taggregates share, such as a condition they all refer to, changed in<br/>
/// place, changes what each of them stores without a tevent on any: every instance, its copy and its committed instance alike,<br/>
/// then takes a snapshot holding the change, so a store finds nothing changed to write, and the committed instance isn't read only<br/>
/// through it. State that is shared belongs in a taggregate of its own, referred to by id, or is changed by a tevent on each<br/>
/// taggregate that holds it. A round trip through the snapshot can't catch this: the copy refers to the same object.</remarks>
///<remarks>bug: a taggregate with entities or shared tentities can't implement <see cref="RestoreFrom"/>: their collections, Tentity's and<br/>
/// SharedTentityCollection, add an entity only by publishing its created tevent, and an instance being restored publishes nothing. So<br/>
/// refactoring a part of a taggregate into entities makes it unusable with a state store (Magnus, 2026-11-17: "fixing a broken one").<br/>
/// Next after the spike's end review: dev_docs/taggregate-improvements/shared/README.md, "Soon, after the end review".</remarks>
public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>
{
   ///<summary>The taggregate's whole state, as it is now.</summary>
   TSnapshot TakeSnapshot();

   ///<summary>Gives this new, empty instance the state <paramref name="snapshot"/> holds, its id first.</summary>
   void RestoreFrom(TSnapshot snapshot);
}
