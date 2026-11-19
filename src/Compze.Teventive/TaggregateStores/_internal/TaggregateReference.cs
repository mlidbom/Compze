using Compze.Abstractions;
using Compze.Teventive.TaggregateStores._private;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>The <see cref="ITaggregateReference{TTaggregate}"/> of a taggregate of one store: asks the store for <see cref="Current"/>.<br/>
/// Equal to every reference to the same taggregate of the same store, whatever class that one was asked for as.</summary>
sealed class TaggregateReference<TTaggregate> : TaggregateReferenceOfAStore, ITaggregateReference<TTaggregate> where TTaggregate : class, ITaggregate
{
   internal TaggregateReference(ITaggregateStore store, TaggregateId id) : base(store, id) {}

   public TTaggregate Current => Store.TryGet(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);
}
