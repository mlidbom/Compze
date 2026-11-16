using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>The <see cref="ITaggregateReference{TTaggregate}"/> of a taggregate of one store: asks the store for <see cref="Current"/>,<br/>
/// and the store's session in the ambient unit of work for <see cref="ForUpdate"/>. Two references to one taggregate are equal.</summary>
sealed class TaggregateReference<TTaggregate> : ITaggregateReference<TTaggregate> where TTaggregate : class, ITaggregate
{
   readonly ITaggregateStore _store;
   readonly TaggregateUnitsOfWork _unitsOfWork;

   internal TaggregateReference(ITaggregateStore store, TaggregateUnitsOfWork unitsOfWork, TaggregateId id)
   {
      _store = store;
      _unitsOfWork = unitsOfWork;
      Id = id;
   }

   public TaggregateId Id { get; }

   public TTaggregate Current => _store.TryGet(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);

   public TTaggregate ForUpdate() => _unitsOfWork.AmbientBeginningIt().SessionOf(_store).TryGetForUpdate(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);

   public override bool Equals(object? other) => other is TaggregateReference<TTaggregate> reference && reference.Id == Id;
   public override int GetHashCode() => Id.GetHashCode();
}
