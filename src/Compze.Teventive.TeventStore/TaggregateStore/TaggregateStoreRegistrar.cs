using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Teventive.TaggregateStore._private;

namespace Compze.Teventive.TaggregateStore;

///<summary>Composes a taggregate store into a container.</summary>
public static class TaggregateStoreRegistrar
{
   ///<summary>Composes a taggregate store that keeps its committed taggregates in memory only: an <see cref="ITaggregateStoreReader"/><br/>
   /// for the container, and an <see cref="ITaggregateStoreUpdater"/> for each unit of work.</summary>
   public static IComponentRegistrar InMemoryTaggregateStore(this IComponentRegistrar @this)
   {
      @this.Register(Singleton.For<ITaggregateStoreReader, InMemoryTaggregateStore>().CreatedBy(() => new InMemoryTaggregateStore()));
      @this.Register(Scoped.For<ITaggregateStoreUpdater>().CreatedBy((InMemoryTaggregateStore store, IUnitOfWorkTeventPublisher teventPublisher) => new TaggregateStoreUpdater(store, teventPublisher)));
      return @this;
   }
}
