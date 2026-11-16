using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.StateStore._private;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;

namespace Compze.Teventive.StateStore;

///<summary>Composes a state store into a container.</summary>
public static class StateStoreRegistrar
{
   ///<summary>Composes a state store, which keeps taggregates as their state, not their history: in memory, and in the storage<br/>
   /// <paramref name="declare"/> declares, if any. Its taggregates are read through the container's <see cref="ITaggregateStoreReader"/>,<br/>
   /// and changed through each unit of work's <see cref="ITaggregateStoreUpdater"/>. The tevents they publish are dispatched through<br/>
   /// the container's Tessaging engine, such as a LocalTessagingEngine, as each publish cascade ends.</summary>
   public static IComponentRegistrar StateStore(this IComponentRegistrar @this, Action<StateStoreBuilder> declare)
   {
      var builder = new StateStoreBuilder(@this);
      declare(builder);
      @this.Register(Singleton.For<IStateStore, TaggregateStateStore>().CreatedBy((TaggregateUnitsOfWork unitsOfWork, IRootResolver resolver) => builder.Build(unitsOfWork, resolver)));
      @this.RegisterTaggregateStore<TaggregateStateStore>();
      return @this;
   }
}
