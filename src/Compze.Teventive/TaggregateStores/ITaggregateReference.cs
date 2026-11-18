using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>Stands for one taggregate of a store for as long as it exists: what code holds across units of work, never an instance.<br/>
/// An instance is one version of the taggregate's state, and a commit that changes the taggregate replaces it.</summary>
///<remarks>A reference only reads: a unit of work changes the taggregate it stands for by getting it for update by its <see cref="Id"/>,<br/>
/// from <see cref="ITaggregateStoreUpdater.GetForUpdate{TTaggregate}"/>. It finds the ambient unit of work from the ambient transaction<br/>
/// alone, so code that holds one needs no session passed down to it to read the unit of work's copy.<br/>
/// Two references are equal when they stand for the same taggregate of the same store, whatever class each was asked for as, so<br/>
/// they can be kept in sets and used as keys.</remarks>
public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ITaggregate
{
   ///<summary>The id of the taggregate this reference stands for.</summary>
   TaggregateId Id { get; }

   ///<summary>Inside a unit of work that holds a copy of the taggregate, that copy; otherwise the committed instance.</summary>
   ///<remarks>Throws once the taggregate's deletion is committed: a reference to a deleted taggregate has nothing to give. So a view<br/>
   /// that may outlive a deletion, such as a row a UI reads between the commit and the notification that reaches it, keeps the last<br/>
   /// instance it got, and refreshes it when notified.</remarks>
   TTaggregate Current { get; }
}
