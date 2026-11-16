using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Teventive.TaggregateStores._internal;

namespace Compze.Teventive.TaggregateStores._private;

///<summary>The <see cref="ITaggregateStoreUpdater"/> of a unit of work's scope: changes the taggregates of each class through the<br/>
/// session of the store that keeps it, in the <see cref="TaggregateUnitOfWork"/> of the ambient transaction.</summary>
sealed class TaggregateStoreUpdater : ITaggregateStoreUpdater
{
   readonly TaggregateStoreRouting _routing;
   readonly TaggregateUnitsOfWork _unitsOfWork;
   readonly TaggregateStoreReader _reader;

   internal TaggregateStoreUpdater(TaggregateStoreRouting routing, TaggregateUnitsOfWork unitsOfWork, TaggregateStoreReader reader)
   {
      _routing = routing;
      _unitsOfWork = unitsOfWork;
      _reader = reader;
   }

   public TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate => _reader.Get<TTaggregate>(id);

   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate =>
      _reader.TryGet(id, out taggregate);

   public ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate => _reader.Reference<TTaggregate>(id);

   public TTaggregate GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
      TryGetForUpdate(id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(id);

   public bool TryGetForUpdate<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate =>
      SessionKeeping(typeof(TTaggregate)).TryGetForUpdate(id, out taggregate);

   public void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate => SessionKeeping(taggregate.GetType()).Save(taggregate);

   public void Delete<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate => SessionKeeping(typeof(TTaggregate)).Delete<TTaggregate>(id);

   ITaggregateStoreSession SessionKeeping(Type taggregateClass) => _unitsOfWork.AmbientBeginningIt().SessionOf(_routing.StoreKeeping(taggregateClass));
}
