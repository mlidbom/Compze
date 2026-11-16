using Compze.Abstractions;
using Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

namespace Compze.Teventive.TaggregateStore._private;

sealed class TaggregateReference<TTaggregate>(InMemoryTaggregateStore store, TaggregateId id) : ITaggregateReference<TTaggregate>
   where TTaggregate : class, ISnapshotTaggregate
{
   public TaggregateId Id { get; } = id;

   public TTaggregate Current
   {
      get
      {
         if(store.AmbientSession is {} session && session.TryGetHeld(Id, out var heldInstance))
            return (TTaggregate)(heldInstance ?? throw new TaggregateNotFoundException(Id)); //Deleted in this unit of work.
         return (TTaggregate)(store.CommittedInstanceOf(Id) ?? throw new TaggregateNotFoundException(Id));
      }
   }

   public TTaggregate ForUpdate() =>
      (store.AmbientSession ?? throw new InvalidOperationException($"Taggregate {Id} can only be changed inside a unit of work that has resolved its {nameof(ITaggregateStoreUpdater)}, and there is none.")).Get<TTaggregate>(Id);

   public override bool Equals(object? other) => other is TaggregateReference<TTaggregate> reference && reference.Id == Id;
   public override int GetHashCode() => Id.GetHashCode();
}
