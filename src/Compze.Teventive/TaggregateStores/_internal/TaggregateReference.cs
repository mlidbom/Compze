using Compze.Abstractions;
using Compze.Teventive.TaggregateStores._private;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>The <see cref="ITaggregateReference{TTaggregate}"/> of a taggregate of one store: asks the store for <see cref="Current"/>,<br/>
/// and the store's session in the ambient unit of work for <see cref="ForUpdate"/>. Equal to every reference to the same taggregate of<br/>
/// the same store, whatever class that one was asked for as.</summary>
sealed class TaggregateReference<TTaggregate> : TaggregateReferenceOfAStore, ITaggregateReference<TTaggregate> where TTaggregate : class, ITaggregate
{
   readonly TaggregateUnitsOfWork _unitsOfWork;

   internal TaggregateReference(ITaggregateStore store, TaggregateUnitsOfWork unitsOfWork, TaggregateId id) : base(store, id) => _unitsOfWork = unitsOfWork;

   public TTaggregate Current => Store.TryGet(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);

   public TTaggregate ForUpdate() => _unitsOfWork.AmbientBeginningIt().SessionOf(Store).TryGetForUpdate(Id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(Id);
}
