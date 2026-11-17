using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>What every <see cref="TaggregateReference{TTaggregate}"/> is, whatever class it was asked for as: a reference to one taggregate<br/>
/// of one store. Two references are equal when they stand for the same taggregate of the same store.</summary>
abstract class TaggregateReference
{
   private protected TaggregateReference(ITaggregateStore store, TaggregateId id)
   {
      Store = store;
      Id = id;
   }

   private protected ITaggregateStore Store { get; }

   public TaggregateId Id { get; }

   public sealed override bool Equals(object? other) => other is TaggregateReference reference && ReferenceEquals(reference.Store, Store) && reference.Id == Id;
   public sealed override int GetHashCode() => Id.GetHashCode();
}

///<summary>The <see cref="ITaggregateReference{TTaggregate}"/> of a taggregate of one store: asks the store for <see cref="Current"/>,<br/>
/// and the store's session in the ambient unit of work for <see cref="ForUpdate"/>.</summary>
sealed class TaggregateReference<TTaggregate> : TaggregateReference, ITaggregateReference<TTaggregate> where TTaggregate : class, ITaggregate
{
   readonly TaggregateUnitsOfWork _unitsOfWork;

   internal TaggregateReference(ITaggregateStore store, TaggregateUnitsOfWork unitsOfWork, TaggregateId id) : base(store, id) => _unitsOfWork = unitsOfWork;

   public TTaggregate Current => Store.TryGet(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);

   public TTaggregate ForUpdate() => _unitsOfWork.AmbientBeginningIt().SessionOf(Store).TryGetForUpdate(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);
}
