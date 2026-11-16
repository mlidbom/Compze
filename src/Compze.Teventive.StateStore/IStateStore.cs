using Compze.Abstractions;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.TaggregateStores;

namespace Compze.Teventive.StateStore;

///<summary>What a state store does as a whole, beyond reading and changing its taggregates through <see cref="ITaggregateStoreReader"/><br/>
/// and <see cref="ITaggregateStoreUpdater"/>. Resolved from the container that composes the store.</summary>
public interface IStateStore
{
   ///<summary>Loads every taggregate from the storage again, as when a program other than this one has changed what the storage holds,<br/>
   /// and replaces the committed taggregates with what it loaded. Dispatches no tevent.</summary>
   ///<remarks>Waits for the units of work open in the store to complete, and keeps new ones from starting until it is done. Every<br/>
   /// <see cref="ITaggregateReference{TTaggregate}"/> is kept, and gives the reloaded state; the indexes file the reloaded state.<br/>
   /// A taggregate counts as changed when the snapshot loaded doesn't equal the snapshot of the instance it replaces, so snapshot types<br/>
   /// need value equality: an override of <see cref="object.Equals(object)"/>.</remarks>
   ///<returns>Which taggregates the storage held that the store didn't, which it held with another state, and which it no longer holds.</returns>
   ReloadedTaggregates ReloadFromStorage();

   ///<summary>As <see cref="ReloadFromStorage"/>, after running <paramref name="changeToTheStorage"/> once the open units of work have<br/>
   /// completed, while new ones wait: such as a fast-forward of the checkout that holds the consumer's files, which no unit of work may<br/>
   /// write a taggregate over.</summary>
   ///<remarks>If <paramref name="changeToTheStorage"/> throws, nothing is reloaded, and the exception comes out of this call.</remarks>
   ReloadedTaggregates ReloadFromStorageAfter(Action changeToTheStorage);

   ///<summary>A reference to every <typeparamref name="TTaggregate"/> the store holds, as an index's lookup gives them.</summary>
   ///<remarks>Inside a unit of work, the unit of work's view: the taggregates it has saved are included, and those it has deleted are<br/>
   /// left out. Outside one, the committed taggregates.</remarks>
   IReadOnlyList<ITaggregateReference<TTaggregate>> All<TTaggregate>() where TTaggregate : class, ITaggregate;
}

///<summary>How what <see cref="ITaggregateStorage"/> held differed from what the state store held, as <see cref="IStateStore.ReloadFromStorage"/> found it.</summary>
public sealed class ReloadedTaggregates
{
   internal ReloadedTaggregates(IReadOnlyList<TaggregateId> added, IReadOnlyList<TaggregateId> changed, IReadOnlyList<TaggregateId> removed)
   {
      Added = added;
      Changed = changed;
      Removed = removed;
   }

   ///<summary>The taggregates the storage held that the store didn't.</summary>
   public IReadOnlyList<TaggregateId> Added { get; }

   ///<summary>The taggregates the storage held with another state than the store did.</summary>
   public IReadOnlyList<TaggregateId> Changed { get; }

   ///<summary>The taggregates the store held that the storage no longer did.</summary>
   public IReadOnlyList<TaggregateId> Removed { get; }
}
