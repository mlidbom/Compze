using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>Stands for one taggregate of a store for as long as it exists: what code holds across units of work, never an instance.<br/>
/// An instance is one version of the taggregate's state, and a commit that changes the taggregate replaces it.</summary>
///<remarks>A reference finds the ambient unit of work from the ambient transaction alone, so code that holds one needs no session<br/>
/// passed down to it.</remarks>
public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ITaggregate
{
   ///<summary>The id of the taggregate this reference stands for.</summary>
   TaggregateId Id { get; }

   ///<summary>Inside a unit of work that holds a copy of the taggregate, that copy; otherwise the committed instance.</summary>
   ///<remarks>Throws once the taggregate's deletion is committed: a reference to a deleted taggregate has nothing to give. So a view<br/>
   /// that may outlive a deletion, such as a row a UI reads between the commit and the notification that reaches it, keeps the last<br/>
   /// instance it got, and refreshes it when notified.</remarks>
   TTaggregate Current { get; }

   ///<summary>The ambient unit of work's copy of the taggregate, which it may change, as <see cref="ITaggregateStoreUpdater.GetForUpdate{TTaggregate}"/><br/>
   /// gets it. Throws when there is no ambient unit of work.</summary>
   TTaggregate ForUpdate();
}
