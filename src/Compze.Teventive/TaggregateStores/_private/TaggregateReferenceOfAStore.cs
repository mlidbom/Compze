using Compze.Abstractions;
using Compze.Teventive.TaggregateStores._internal;

namespace Compze.Teventive.TaggregateStores._private;

///<summary>What every <see cref="TaggregateReference{TTaggregate}"/> is, whatever class it was asked for as: a reference to one taggregate<br/>
/// of one store. Two references are equal when they stand for the same taggregate of the same store.</summary>
abstract class TaggregateReferenceOfAStore
{
   //review-naming: TaggregateReferenceOfAStore, a name of the end review's fixes (2026-11-18).

   private protected TaggregateReferenceOfAStore(ITaggregateStore store, TaggregateId id)
   {
      Store = store;
      Id = id;
   }

   private protected ITaggregateStore Store { get; }

   public TaggregateId Id { get; }

   public sealed override bool Equals(object? other) => other is TaggregateReferenceOfAStore reference && ReferenceEquals(reference.Store, Store) && reference.Id == Id;
   public sealed override int GetHashCode() => Id.GetHashCode();
}
