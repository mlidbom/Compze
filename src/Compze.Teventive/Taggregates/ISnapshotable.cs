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
///<remarks>A collection of entities, or of shared tentities, is restored by adding each entity with its collection's <c>AddRestored</c>, which<br/>
/// gives it its id and publishes nothing, and then giving it the rest of its state, as a component is given its.</remarks>
public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>
{
   ///<summary>The taggregate's whole state, as it is now.</summary>
   TSnapshot TakeSnapshot();

   ///<summary>Gives this new, empty instance the state <paramref name="snapshot"/> holds, its id first.</summary>
   void RestoreFrom(TSnapshot snapshot);
}
