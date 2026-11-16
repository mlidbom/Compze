using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.TaggregateStores._private;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>Registers a taggregate store with a container, and, with the first store, what every store shares: the<br/>
/// <see cref="ITaggregateStoreReader"/> and <see cref="ITaggregateStoreBulkReader"/>, the <see cref="ITaggregateStoreUpdater"/> of each<br/>
/// unit of work, and the <see cref="TaggregateUnitsOfWork"/>.</summary>
static class TaggregateStoresRegistrar
{
   ///<summary>Makes <typeparamref name="TStore"/>, registered as a singleton by the store's own registrar, one of the container's<br/>
   /// taggregate stores.</summary>
   internal static void RegisterTaggregateStore<TStore>(this IComponentRegistrar registrar) where TStore : class, ITaggregateStore
   {
      if(!registrar.IsRegistered<TaggregateUnitsOfWork>()) RegisterWhatTheStoresShare(registrar);
      registrar.Register(Singleton.ForSet<ITaggregateStore>().CreatedBy((TStore store) => store));
   }

   static void RegisterWhatTheStoresShare(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<TaggregateUnitsOfWork>().CreatedBy(() => new TaggregateUnitsOfWork()),
         Singleton.For<TaggregateStoreRouting>().CreatedBy((IComponentSet<ITaggregateStore> stores) => new TaggregateStoreRouting(stores)),
         Singleton.For<ITaggregateStoreReader, ITaggregateStoreBulkReader, TaggregateStoreReader>().CreatedBy((TaggregateStoreRouting routing) => new TaggregateStoreReader(routing)),
         Scoped.For<ITaggregateStoreUpdater>().CreatedBy((TaggregateStoreRouting routing, TaggregateUnitsOfWork unitsOfWork, TaggregateStoreReader reader) => new TaggregateStoreUpdater(routing, unitsOfWork, reader)));
}
