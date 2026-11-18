using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.MemoryImageStateStore._private;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>Composes a memory image state store into a container: a memory image of taggregates, every one held in memory for the<br/>
/// container's life and rebuilt at its start from a snapshot of each, which its storage keeps.</summary>
public static class MemoryImageStateStoreRegistrar
{
   ///<summary>Composes a memory image state store: a memory image of taggregates, every one held in memory for the container's life and<br/>
   /// rebuilt at its start from a snapshot of each, which the storage <paramref name="declare"/> declares keeps, if it declares any. It<br/>
   /// keeps taggregates as their state, not their history. Its taggregates are read through the container's <see cref="ITaggregateStoreReader"/>,<br/>
   /// and changed through each unit of work's <see cref="ITaggregateStoreUpdater"/>. The tevents they publish are dispatched through<br/>
   /// the container's Tessaging engine, such as a LocalTessagingEngine, as each publish cascade ends.</summary>
   public static IComponentRegistrar MemoryImageStateStore(this IComponentRegistrar @this, Action<MemoryImageStateStoreBuilder> declare)
   {
      var builder = new MemoryImageStateStoreBuilder(@this);
      declare(builder);
      builder.AssertDeclarationsFitTogether();
      @this.Register(Singleton.For<IMemoryImageStateStore, TaggregateMemoryImageStateStore>().CreatedBy((TaggregateUnitsOfWork unitsOfWork, IRootResolver resolver) => builder.Build(unitsOfWork, resolver)));
      @this.RegisterTaggregateStore<TaggregateMemoryImageStateStore>();
      return @this;
   }
}
