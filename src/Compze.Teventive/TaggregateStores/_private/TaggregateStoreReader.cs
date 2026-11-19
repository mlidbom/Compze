using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Teventive.TaggregateStores._internal;

namespace Compze.Teventive.TaggregateStores._private;

///<summary>The <see cref="ITaggregateStoreReader"/> and <see cref="ITaggregateStoreBulkReader"/> of a container: asks the store that keeps<br/>
/// each class.</summary>
sealed class TaggregateStoreReader : ITaggregateStoreBulkReader
{
   readonly TaggregateStoreRouting _routing;

   internal TaggregateStoreReader(TaggregateStoreRouting routing) => _routing = routing;

   public TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
      TryGet(id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(id);

   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate =>
      _routing.StoreKeeping(typeof(TTaggregate)).TryGet(id, out taggregate);

   public ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
      _routing.StoreKeeping(typeof(TTaggregate)).Reference<TTaggregate>(id);

   public IReadOnlyCollection<ITaggregateReference<TTaggregate>> ReferencesToAll<TTaggregate>() where TTaggregate : class, ITaggregate =>
      _routing.StoreKeeping(typeof(TTaggregate)).ReferencesToAll<TTaggregate>();
}
