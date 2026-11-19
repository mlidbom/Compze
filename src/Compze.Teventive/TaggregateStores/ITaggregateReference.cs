using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>Stands for one taggregate of a store for as long as it exists: what code holds across units of work, never an instance.<br/>
/// An instance is one version of the taggregate's state, and a commit that changes the taggregate replaces it.</summary>
///<remarks>A reference only reads: a unit of work changes the taggregate it stands for by getting it for update by its <see cref="Id"/>,<br/>
/// from <see cref="ITaggregateStoreUpdater.GetForUpdate{TTaggregate}"/>. It finds the ambient unit of work from the ambient transaction<br/>
/// alone, so code that holds one needs no session passed down to it to read the unit of work's own instance.<br/>
/// Two references are equal when they stand for the same taggregate of the same store, whatever class each was asked for as, so<br/>
/// they can be kept in sets and used as keys.</remarks>
public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ITaggregate
{
   ///<summary>The id of the taggregate this reference stands for.</summary>
   TaggregateId Id { get; }

   ///<summary>The taggregate now, as <see cref="ITaggregateStoreReader.Get{TTaggregate}"/> gets it: inside a unit of work that holds it,<br/>
   /// the unit of work's own instance; otherwise the taggregate as last committed, read only.</summary>
   ///<remarks>Each read of it is a read of the store, at the store's cost: for the memory image state store a lookup of the one instance<br/>
   /// it holds, for the tevent store a new instance built from the taggregate's history. So code that uses the taggregate several<br/>
   /// times in a row reads it once.</remarks>
   ///<remarks>Throws an <see cref="ArgumentOutOfRangeException"/> while no store holds the taggregate: before it is added, and after it is<br/>
   /// deleted. Inside the unit of work that deletes it, that is from its <see cref="Taggregates.Tevents.ITaggregateDeletedTevent"/> on;<br/>
   /// everywhere else, once that unit of work has committed, and until then the taggregate as last committed is read.<br/>
   /// So a view that may outlive a deletion, such as a row a UI reads between the commit and the notification that reaches it, keeps the<br/>
   /// last instance it got, and refreshes it when notified.</remarks>
   TTaggregate Current { get; }
}
