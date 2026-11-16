using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Teventive.TaggregateStore._private;

namespace Compze.Teventive.TaggregateStore;

///<summary>Composes a taggregate store into a container.</summary>
public static class TaggregateStoreRegistrar
{
   ///<summary>Composes a taggregate store that keeps its committed taggregates in memory only: an <see cref="ITaggregateStoreReader"/><br/>
   /// for the container, an <see cref="ITaggregateStoreUpdater"/> for each unit of work, and what <paramref name="build"/> declares.</summary>
   public static IComponentRegistrar InMemoryTaggregateStore(this IComponentRegistrar @this, Action<TaggregateStoreBuilder>? build = null)
   {
      var store = new InMemoryTaggregateStore();
      build?.Invoke(new TaggregateStoreBuilder(store, @this));
      @this.Register(Singleton.For<ITaggregateStoreReader, InMemoryTaggregateStore>().Instance(store));
      @this.Register(Scoped.For<ITaggregateStoreUpdater>().CreatedBy((InMemoryTaggregateStore theStore, IUnitOfWorkTeventPublisher teventPublisher) => new TaggregateStoreUpdater(theStore, teventPublisher)));
      return @this;
   }
}
