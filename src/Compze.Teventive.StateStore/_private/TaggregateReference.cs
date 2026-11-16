using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;

namespace Compze.Teventive.StateStore._private;

sealed class TaggregateReference<TTaggregate> : ITaggregateReference<TTaggregate> where TTaggregate : class, ITaggregate
{
   readonly TaggregateStateStore _store;

   internal TaggregateReference(TaggregateStateStore store, TaggregateId id)
   {
      _store = store;
      Id = id;
   }

   public TaggregateId Id { get; }

   public TTaggregate Current => _store.TryGet(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);

   public TTaggregate ForUpdate() => _store.SessionOfTheAmbientUnitOfWork().TryGetForUpdate(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);

   public override bool Equals(object? other) => other is TaggregateReference<TTaggregate> reference && reference.Id == Id;
   public override int GetHashCode() => Id.GetHashCode();
}
